/**
 * Application configuration object
 */
export const config = {
  auth: {
    clientId: 'e4fe47ef-4e88-418f-a598-d1d19d2dfb67',
    authority: 'https://login.microsoftonline.com/consumers',
    redirectUri: 'http://localhost:1234/auth',
    postLogoutRedirectUri: 'http://localhost:1234/logout',
  },
  app: {
    name: 'My Tools',
    version: '1.0.0',
  },
};
