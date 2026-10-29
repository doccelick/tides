import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { TidesResponse } from "@/api/generated";
import { TidePanel } from "./TidePanel";

const tides: TidesResponse = {
  timeZone: "UTC",
  unit: "cm",
  reference: "CD",
  tides: [
    { type: "High", time: "2026-10-19T03:48:00+00:00", levelCm: 223 },
    { type: "Low", time: "2026-10-19T09:45:00+00:00", levelCm: 143 },
  ],
};

describe("TidePanel", () => {
  it("shows the tides when they load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(tides)));
    render(<TidePanel code="BGO" />);

    expect(await screen.findByText("High")).toBeInTheDocument();
    expect(screen.getByText("Low")).toBeInTheDocument();
  });

  it("shows a message when the station has no tide data", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    render(<TidePanel code="BGO" />);

    expect(await screen.findByText("No tide data for this station.")).toBeInTheDocument();
  });

  it("shows a notice when the tides fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<TidePanel code="BGO" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load tides");
  });
});
