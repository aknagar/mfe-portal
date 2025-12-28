import React from 'react';

export const HelloWorld = () => {
  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-4">Hello World</h1>
      <p className="text-lg">Welcome to the Hello World pilet!</p>
      <p className="mt-2 text-muted-foreground">
        This is a micro-frontend module loaded into the Piral shell.
      </p>
    </div>
  );
};