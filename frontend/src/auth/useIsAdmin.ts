import type { AccountInfo } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";

// The API checks the role on every write. In the app, the role only decides what is shown.
export function findAdminAccount(accounts: AccountInfo[]): AccountInfo | undefined {
  return accounts.find((account) => account.idTokenClaims?.roles?.includes("Admin"));
}

export function useIsAdmin(): boolean {
  const { accounts } = useMsal();
  return findAdminAccount(accounts) !== undefined;
}
