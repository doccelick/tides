import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { WaterLevelResponse } from "@/api/generated";
import { CurrentLevel } from "./CurrentLevel";

const waterLevel: WaterLevelResponse = {
  stationCode: "BOO",
  timeZone: "UTC",
  unit: "cm",
  reference: "CD",
  time: "2026-10-20T20:10:00+00:00",
  levelCm: 207.9,
};

describe("CurrentLevel", () => {
  it("shows the level and when it was measured", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(waterLevel)));
    render(<CurrentLevel code="BOO" />);

    expect(await screen.findByText("208 cm")).toBeInTheDocument();
    expect(screen.getByText(/measured 22:10/)).toBeInTheDocument();
  });

  it("shows nothing when the station has no water level", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    const { container } = render(<CurrentLevel code="BOO" />);

    await waitFor(() => expect(container).toBeEmptyDOMElement());
  });

  it("shows a notice when the water level fails to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<CurrentLevel code="BOO" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load water level");
  });
});
