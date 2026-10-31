import type { AccountInfo } from "@azure/msal-browser";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getAdminToken } from "./getAdminToken";

const msal = vi.hoisted(() => ({
  getAllAccounts: vi.fn(),
  acquireTokenSilent: vi.fn(),
}));

vi.mock("./authConfig", () => ({ apiScope: "api://tides/access_as_user", msalInstance: msal }));

const admin = {
  homeAccountId: "admin",
  environment: "login.microsoftonline.com",
  tenantId: "tenant",
  username: "admin@tides.test",
  localAccountId: "admin",
  idTokenClaims: { roles: ["Admin"] },
} satisfies AccountInfo;

describe("getAdminToken", () => {
  beforeEach(() => {
    msal.getAllAccounts.mockReset();
    msal.acquireTokenSilent.mockReset();
  });

  it("returns an access token for the API scope and the admin account", async () => {
    msal.getAllAccounts.mockReturnValue([admin]);
    msal.acquireTokenSilent.mockResolvedValue({ accessToken: "token-1" });

    expect(await getAdminToken()).toBe("token-1");
    expect(msal.acquireTokenSilent).toHaveBeenCalledWith({
      scopes: ["api://tides/access_as_user"],
      account: admin,
    });
  });

  it("returns nothing when no admin is signed in", async () => {
    msal.getAllAccounts.mockReturnValue([]);

    expect(await getAdminToken()).toBeUndefined();
    expect(msal.acquireTokenSilent).not.toHaveBeenCalled();
  });

  it("returns nothing when the session has expired", async () => {
    msal.getAllAccounts.mockReturnValue([admin]);
    msal.acquireTokenSilent.mockRejectedValue(new Error("interaction_required"));

    expect(await getAdminToken()).toBeUndefined();
  });
});
