import * as React from 'react';
import type { PiletApi } from 'mfe-portal-shell';
import { UrlGetter } from './UrlGetter';

export function setup(api: PiletApi) {
  api.registerPage('/url-getter', UrlGetter, {
    meta: {
      title: 'URL Getter',
    },
  });
}
