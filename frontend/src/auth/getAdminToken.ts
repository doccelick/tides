import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { apiScope, msalInstance } from "./authConfig";
import { findAdminAccount } from "./useIsAdmin";

// Returns undefined when no admin is signed in or the token request fails. The request then
// goes without a token, and the API answers 401.
export async function getAdminToken(): Promise<string | undefined> {
  const account = findAdminAccount(msalInstance.getAllAccounts());
  if (account === undefined) return undefined;

  const request = { scopes: [apiScope], account };

  try {
    const result = await msalInstance.acquireTokenSilent(request);
    return result.accessToken;
  } catch (error) {
    // The Entra session has ended. The redirect leaves the page, and MsalProvider handles the
    // response when the app loads again.
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.acquireTokenRedirect(request);
    }
    return undefined;
  }
}
