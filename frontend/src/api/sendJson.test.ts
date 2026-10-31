import { describe, expect, it, vi } from "vitest";
import { sendJson } from "./sendJson";

describe("sendJson", () => {
  it("sends the body as JSON", async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json({ id: 3 }, { status: 201 }));
    vi.stubGlobal("fetch", fetch);

    await sendJson("POST", "/v1/locations", { name: "Kjerringøy" });

    expect(fetch).toHaveBeenCalledWith("/v1/locations", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: "Kjerringøy" }),
    });
  });

  it("sends the token as a bearer token", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetch);

    await sendJson("DELETE", "/v1/imports/12", undefined, "token-1");

    expect(fetch).toHaveBeenCalledWith("/v1/imports/12", {
      method: "DELETE",
      headers: { Authorization: "Bearer token-1" },
    });
  });

  it("asks to sign in again on 401 Unauthorized", async () => {
    const problem = { status: 401, title: "Unauthorized" };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(problem, { status: 401 })));

    const result = await sendJson("DELETE", "/v1/imports/12");

    expect(result).toEqual({
      ok: false,
      error: { message: "Sign in again to save changes", fieldErrors: {} },
    });
  });

  it("returns the response body on success", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({ id: 3 }, { status: 201 })));

    const result = await sendJson("POST", "/v1/locations", {});

    expect(result).toEqual({ ok: true, data: { id: 3 } });
  });

  it("succeeds without a body on 204 No Content", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    const result = await sendJson("DELETE", "/v1/imports/12");

    expect(result).toEqual({ ok: true, data: undefined });
  });

  it("returns an error when a successful response is not JSON", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("<html>", { status: 200 })));

    const result = await sendJson("POST", "/v1/locations", {});

    expect(result).toEqual({
      ok: false,
      error: { message: "Unexpected response from the API (200)", fieldErrors: {} },
    });
  });

  it("uses the problem title as the error message", async () => {
    const problem = { status: 422, title: "Too far from the coast." };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(problem, { status: 422 })));

    const result = await sendJson("POST", "/v1/locations", {});

    expect(result).toEqual({
      ok: false,
      error: { message: "Too far from the coast.", fieldErrors: {} },
    });
  });

  it("returns the field errors of a validation problem", async () => {
    const problem = {
      status: 400,
      title: "One or more validation errors occurred.",
      errors: { Name: ["The Name field is required."] },
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(problem, { status: 400 })));

    const result = await sendJson("POST", "/v1/locations", {});

    expect(result).toEqual({
      ok: false,
      error: {
        message: "One or more validation errors occurred.",
        fieldErrors: { Name: ["The Name field is required."] },
      },
    });
  });

  it("names the status when the error has no problem body", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    const result = await sendJson("DELETE", "/v1/locations/3");

    expect(result).toEqual({
      ok: false,
      error: { message: "Request failed with 500", fieldErrors: {} },
    });
  });

  it("reports when the API cannot be reached", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    const result = await sendJson("DELETE", "/v1/locations/3");

    expect(result).toEqual({
      ok: false,
      error: { message: "Couldn't reach the API", fieldErrors: {} },
    });
  });
});
