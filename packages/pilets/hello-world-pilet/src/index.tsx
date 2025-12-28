import * as React from 'react';
import type { PiletApi } from 'portal-shell';
import { setup as helloWorldSetup } from './HelloWorld';

export function setup(api: PiletApi) {
  helloWorldSetup(api);
}
