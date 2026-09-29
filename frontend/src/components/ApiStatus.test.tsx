import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ApiStatus } from "./ApiStatus";

describe("ApiStatus", () => {
  it("shows checking while the request is pending", () => {
    vi.stubGlobal("fetch", vi.fn().mockReturnValue(new Promise(() => {})));

    render(<ApiStatus />);

    expect(screen.getByText("Checking API…")).toBeInTheDocument();
  });

  it("shows online when the health check succeeds", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("Healthy")));

    render(<ApiStatus />);

    expect(await screen.findByText("API online")).toBeInTheDocument();
  });

  it("shows offline when the health check returns an error status", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    render(<ApiStatus />);

    expect(await screen.findByText("API offline")).toBeInTheDocument();
  });

  it("shows offline when the request fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    render(<ApiStatus />);

    expect(await screen.findByText("API offline")).toBeInTheDocument();
  });
});
