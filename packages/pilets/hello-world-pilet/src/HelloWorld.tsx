import * as React from 'react';
import { PiletApi } from "portal-shell";

const HelloWorldPage: React.FC = () => {
  return <div>Hello, Piral!</div>;
};

export function setup(app: PiletApi) {
  app.registerPage('/hello-world', HelloWorldPage, {
    meta: {
      title: 'Hello World Page',
    },
  });
}