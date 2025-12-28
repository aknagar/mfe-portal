import * as React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Wrench, LogIn, CheckCircle } from 'lucide-react';

export const Logout: React.FC = () => {
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch((e) => {
      console.error('Login failed:', e);
    });
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 flex items-center justify-center">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="flex items-center justify-center gap-3 mb-6">
            <Wrench className="h-12 w-12 text-primary" />
            <h1 className="text-5xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-primary to-purple-600">
              My Tools
            </h1>
          </div>
        </div>

        {/* Logout Confirmation Card */}
        <Card className="max-w-md mx-auto shadow-xl">
          <CardHeader className="text-center">
            <div className="flex justify-center mb-4">
              <CheckCircle className="h-16 w-16 text-green-500" />
            </div>
            <CardTitle className="text-2xl">You've been logged out</CardTitle>
            <CardDescription className="text-base">
              Thank you for using My Tools. Your session has ended securely.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Button 
              onClick={handleLogin} 
              size="lg" 
              className="w-full gap-2 text-lg py-6"
            >
              <LogIn className="h-5 w-5" />
              Sign in again
            </Button>
            <p className="text-xs text-center text-muted-foreground">
              Supports @outlook.com, @hotmail.com, @live.com accounts
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
