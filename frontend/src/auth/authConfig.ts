import { PublicClientApplication } from "@azure/msal-browser";

// Entra IDs of the Tides tenant and app registrations. They are visible in every token, so
// they are configuration and not secrets.
const tenantId = "470f4334-a9c7-481b-99d3-734444df6a7a";
const spaClientId = "e306aa90-9aa0-4648-a8f0-6866caec6fea";
const apiClientId = "f0218090-f845-4c2b-93e1-7f48c1830adb";

export const apiScope = `api://${apiClientId}/access_as_user`;

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: spaClientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
});
