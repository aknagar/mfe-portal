import * as React from 'react';
import { createRoot } from 'react-dom/client';
import { createInstance, Piral, SetRoute } from 'piral';
import { MsalProvider, useMsal } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { layout, errors } from './layout';
import { Dashboard } from './pages/Dashboard';
import { Users } from './pages/Users';
import { Settings } from './pages/Settings';
import { Login } from './pages/Login';
import { Logout } from './pages/Logout';
import { Auth } from './pages/Auth';
import { HelloWorld } from './pages/HelloWorld';
import { UrlGetter } from './pages/UrlGetter';
import { ProductTable } from './pages/ProductTable';
import { msalConfig } from './authConfig';
import { config } from './config';
import './index.css';

console.log('Starting application...');

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

const instance = createInstance({
  state: {
    components: layout,
    errorComponents: errors
  },
  plugins: [],
  requestPilets() {
    // No dynamic pilet loading due to Vite 6 incompatibility
    // Pilets are registered as regular routes instead
    return Promise.resolve([]);
  },
});

console.log('Piral instance created:', instance);

// Initialize MSAL
const initializeMsal = async () => {
  await msalInstance.initialize();
  await msalInstance.handleRedirectPromise();
};

const App: React.FC = () => {
  console.log('Rendering App component');
  const { accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;
  const currentPath = window.location.pathname;

  // Show auth redirect page without Piral layout
  if (currentPath === '/auth') {
    return <Auth />;
  }

  // Show logout page without Piral layout
  if (currentPath === '/logout') {
    return <Logout />;
  }

  // Show login page without Piral layout if not authenticated
  if (!isAuthenticated) {
    // Store the intended destination for post-login redirect
    sessionStorage.setItem('postLoginRedirect', currentPath);
    return <Login />;
  }

  return (
    <Piral instance={instance}>
      <SetRoute path="/" component={Dashboard} />
      <SetRoute path="/users" component={Users} />
      <SetRoute path="/products" component={ProductTable} />
      <SetRoute path="/settings" component={Settings} />
      <SetRoute path="/hello-world" component={HelloWorld} />
      <SetRoute path="/api-playground" component={UrlGetter} />
    </Piral>
  );
};

const container = document.querySelector('#app');
console.log('Container element:', container);
if (!container) {
  throw new Error('Root element #app not found');
}
const root = createRoot(container);
console.log('Root created, initializing MSAL and rendering...');

// Initialize MSAL before rendering
initializeMsal().then(() => {
  root.render(
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  );
});
