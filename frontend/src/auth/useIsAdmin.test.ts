import type { AccountInfo } from "@azure/msal-browser";
import { describe, expect, it } from "vitest";
import { findAdminAccount } from "./useIsAdmin";

function account(username: string, roles?: string[]): AccountInfo {
  return {
    homeAccountId: username,
    environment: "login.microsoftonline.com",
    tenantId: "tenant",
    username,
    localAccountId: username,
    idTokenClaims: roles === undefined ? {} : { roles },
  };
}

describe("findAdminAccount", () => {
  it("finds the account with the Admin role", () => {
    const admin = account("admin@tides.test", ["Admin"]);

    expect(findAdminAccount([account("reader@tides.test"), admin])).toBe(admin);
  });

  it("finds nothing when no account has the Admin role", () => {
    expect(findAdminAccount([account("reader@tides.test", ["Reader"])])).toBeUndefined();
  });

  it("finds nothing when no one is signed in", () => {
    expect(findAdminAccount([])).toBeUndefined();
  });

  it("ignores a role with other casing", () => {
    expect(findAdminAccount([account("admin@tides.test", ["admin"])])).toBeUndefined();
  });
});
