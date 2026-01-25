/**
 * Application configuration object
 */
export const config = {
  auth: {
    clientId: 'e4fe47ef-4e88-418f-a598-d1d19d2dfb67',
    authority: 'https://login.microsoftonline.com/consumers',
    redirectUri: '/auth',
    postLogoutRedirectUri: '/logout',
  },
  app: {
    name: 'My Tools',
    version: '1.0.0',
  },
  api: {
    baseUrl: 'https://augmentservice.gentlesmoke-c643e3fb.centralindia.azurecontainerapps.io',
  },
};
