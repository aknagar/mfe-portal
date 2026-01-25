import * as React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';

export const Settings: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Settings</h1>
        <p className="text-muted-foreground mt-2">
          Configure your application settings
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Application Settings</CardTitle>
          <CardDescription>Manage system configuration and preferences</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center py-12 text-muted-foreground">
            <p>Settings panel coming soon...</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
