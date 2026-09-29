import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";

describe("App", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("Healthy")));
  });

  it("renders the start page heading", async () => {
    render(<App />);

    expect(screen.getByRole("heading", { level: 1, name: "Tides" })).toBeInTheDocument();
    await screen.findByText("API online");
  });
});
