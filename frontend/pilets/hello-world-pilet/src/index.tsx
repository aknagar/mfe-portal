import * as React from 'react';
import type { PiletApi } from 'mfe-portal-shell';
import { HelloWorld } from './HelloWorld';

export function setup(api: PiletApi) {
  api.registerPage('/hello-world', HelloWorld, {
    meta: {
      title: 'Hello World Page',
    },
  });
}
