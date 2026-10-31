import { apiScope, msalInstance } from "./authConfig";
import { findAdminAccount } from "./useIsAdmin";

// Returns undefined when no admin is signed in or the session has expired. The request then
// goes without a token, and the API answers 401.
export async function getAdminToken(): Promise<string | undefined> {
  const account = findAdminAccount(msalInstance.getAllAccounts());
  if (account === undefined) return undefined;

  try {
    const result = await msalInstance.acquireTokenSilent({ scopes: [apiScope], account });
    return result.accessToken;
  } catch {
    return undefined;
  }
}
