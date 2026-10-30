import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { EstimatedWaterLevelResponse, WaterLevelResponse } from "@/api/generated";
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
    render(<CurrentLevel path="/v1/stations/BOO" />);

    expect(await screen.findByText("208 cm")).toBeInTheDocument();
    expect(screen.getByText(/measured 22:10/)).toBeInTheDocument();
  });

  it("shows an estimate with the station it comes from", async () => {
    const estimate: EstimatedWaterLevelResponse = {
      locationId: 7,
      timeZone: "UTC",
      unit: "cm",
      reference: "CD",
      time: "2026-11-01T00:20:00+00:00",
      levelCm: 241.0,
      stationCode: "BOO",
      stationName: "Bodø",
      distanceKm: 4.7,
      weatherEffectCm: 11.0,
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(estimate)));
    render(<CurrentLevel path="/v1/locations/7" />);

    expect(await screen.findByText("241 cm")).toBeInTheDocument();
    expect(screen.getByText(/estimated from Bodø, 4.7 km away, 01:20/)).toBeInTheDocument();
  });

  it("shows nothing when the station has no water level", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    const { container } = render(<CurrentLevel path="/v1/stations/BOO" />);

    await waitFor(() => expect(container).toBeEmptyDOMElement());
  });

  it("shows a notice when the water level fails to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<CurrentLevel path="/v1/stations/BOO" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load water level");
  });
});
