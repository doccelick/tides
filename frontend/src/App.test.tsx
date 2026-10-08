import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";

vi.mock("@/map/MapPage", () => ({
  MapPage: () => <div>Map page</div>,
}));

describe("App", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("Healthy")));
  });

  it("renders the map with API status", async () => {
    render(<App />);

    expect(screen.getByText("Map page")).toBeInTheDocument();
    await screen.findByText("API online");
  });
});
