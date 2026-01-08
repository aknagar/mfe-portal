import React from 'react';

export const UrlGetter = () => {
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">API Playground</h1>
      <p className="text-lg">Test your API endpoints here!</p>
      <p className="mt-2 text-muted-foreground">
        This is where you can make HTTP requests and view responses.
      </p>
    </div>
  );
};
