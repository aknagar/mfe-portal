import * as React from 'react';
import { useMsal } from '@azure/msal-react';

export const Auth: React.FC = () => {
  const { instance, accounts } = useMsal();

  React.useEffect(() => {
    const handleRedirect = async () => {
      try {
        // Handle the redirect response
        const response = await instance.handleRedirectPromise();
        
        // If authenticated, redirect to dashboard
        if (accounts.length > 0) {
          window.location.href = '/';
        }
      } catch (error) {
        console.error('Authentication error:', error);
        // On error, redirect to home which will show login
        window.location.href = '/';
      }
    };

    handleRedirect();
  }, [instance, accounts]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <h1 className="text-2xl font-bold mb-4">Authenticating...</h1>
        <p className="text-muted-foreground">Please wait while we complete your sign-in.</p>
      </div>
    </div>
  );
};
