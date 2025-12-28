import * as React from 'react';
import { ComponentsState, ErrorComponentsState } from 'piral';
import { AdminLayout } from './components/AdminLayout';

export const errors: Partial<ErrorComponentsState> = {
  not_found: () => (
    <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <h1 className="text-4xl font-bold mb-4">404</h1>
        <p className="text-muted-foreground">Page not found</p>
      </div>
    </div>
  ),
};

export const layout: Partial<ComponentsState> = {
  Layout: ({ children }) => <AdminLayout>{children}</AdminLayout>,
  ErrorInfo: (props) => {
    console.error('Piral Error:', props);
    return (
      <div className="p-4 bg-destructive/10 text-destructive rounded-md">
        <h2 className="font-bold">Error</h2>
        <pre className="text-xs mt-2 overflow-auto">{JSON.stringify(props, null, 2)}</pre>
      </div>
    );
  },
  DashboardContainer: () => null,
  DashboardTile: () => null,
};
