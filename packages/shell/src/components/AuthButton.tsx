import * as React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';
import { Button } from './ui/button';
import { LogIn, LogOut, User } from 'lucide-react';

export const AuthButton: React.FC = () => {
  const { instance, accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch((e) => {
      console.error(e);
    });
  };

  const handleLogout = () => {
    instance.logoutRedirect({
      postLogoutRedirectUri: import.meta.env.VITE_MSFT_POST_LOGOUT_REDIRECT_URI || 'http://localhost:1234/logout'
    }).catch((e) => {
      console.error(e);
    });
  };

  if (isAuthenticated) {
    return (
      <Button
        variant="outline"
        size="sm"
        onClick={handleLogout}
        className="gap-2"
      >
        <LogOut className="h-4 w-4" />
        <span className="hidden md:inline">Logout</span>
      </Button>
    );
  }

  return (
    <Button onClick={handleLogin} variant="default" size="sm" className="gap-2">
      <LogIn className="h-4 w-4" />
      <span className="hidden md:inline">Login</span>
    </Button>
  );
};
