import { describe, expect, it, vi } from "vitest";
import { createImport } from "./importRequests";

vi.mock("@/auth/getAdminToken", () => ({ getAdminToken: () => Promise.resolve("token-1") }));

describe("createImport", () => {
  it("posts without a body, since the API picks the months", async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json({ imports: [] }, { status: 201 }));
    vi.stubGlobal("fetch", fetch);

    await createImport(3);

    expect(fetch).toHaveBeenCalledWith("/v1/locations/3/imports", {
      method: "POST",
      headers: { Authorization: "Bearer token-1" },
    });
  });
});
