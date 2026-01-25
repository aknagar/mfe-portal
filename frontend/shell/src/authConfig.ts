import { Configuration, LogLevel } from '@azure/msal-browser';
import { config } from './config';

/**
 * Configuration object to be passed to MSAL instance on creation.
 * Configured for Microsoft Sign-In (personal and work/school accounts)
 * For a full list of MSAL.js configuration parameters, visit:
 * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/configuration.md
 */
export const msalConfig: Configuration = {
  auth: {
    clientId: config.auth.clientId,
    authority: config.auth.authority,
    redirectUri: `${window.location.origin}${config.auth.redirectUri}`,
    postLogoutRedirectUri: `${window.location.origin}${config.auth.postLogoutRedirectUri}`,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: 'localStorage', // This configures where your cache will be stored
    storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
          default:
            return;
        }
      },
    },
  },
};

/**
 * Scopes you add here will be prompted for user consent during sign-in.
 * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
 * For Microsoft Sign-In, User.Read gives access to basic profile information.
 */
export const loginRequest = {
  scopes: ['User.Read'],
  prompt: 'select_account', // Always show account picker for Microsoft Sign-In
};

/**
 * Add here the scopes to request when obtaining an access token for MS Graph API.
 */
export const graphConfig = {
  graphMeEndpoint: 'https://graph.microsoft.com/v1.0/me',
};
