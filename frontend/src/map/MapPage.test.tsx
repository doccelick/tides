import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { MapPage } from "./MapPage";
import type { StationResponse } from "@/api/generated";

vi.mock("react-map-gl/maplibre", () => ({
  default: ({
    children,
    attributionControl,
  }: {
    children: React.ReactNode;
    attributionControl: { customAttribution: string };
  }) => (
    <div data-testid="map" data-attribution={attributionControl.customAttribution}>
      {children}
    </div>
  ),
  Marker: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Source: ({ tiles }: { tiles: string[] }) => (
    <div data-testid="kartverket-source" data-tiles={tiles[0]} />
  ),
  Layer: () => null,
}));

const stations: StationResponse[] = [
  { code: "BGO", name: "Bergen", latitude: 60.4, longitude: 5.3 },
  { code: "OSL", name: "Oslo", latitude: 59.9, longitude: 10.7 },
];

describe("MapPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({ stations })));
  });

  it("starts on the topo map", () => {
    render(<MapPage />);

    expect(screen.getByRole("button", { name: "Map", pressed: true })).toBeInTheDocument();
    expect(screen.getByTestId("kartverket-source").getAttribute("data-tiles")).toContain("/topo/");
  });

  it("switches to the nautical chart", async () => {
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(screen.getByRole("button", { name: "Nautical chart" }));

    expect(
      screen.getByRole("button", { name: "Nautical chart", pressed: true }),
    ).toBeInTheDocument();
    expect(screen.getByTestId("kartverket-source").getAttribute("data-tiles")).toContain(
      "/sjokartraster/",
    );
  });

  it("shows the not-for-navigation note", () => {
    render(<MapPage />);

    expect(screen.getByTestId("map")).toHaveAttribute("data-attribution", "Not for navigation");
  });

  it("shows a marker for each station", async () => {
    render(<MapPage />);

    expect(await screen.findByRole("button", { name: "Bergen" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Oslo" })).toBeInTheDocument();
  });

  it("opens the station card when a marker is tapped and closes it again", async () => {
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(await screen.findByRole("button", { name: "Bergen" }));
    expect(screen.getByRole("region", { name: "Bergen" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("region", { name: "Bergen" })).not.toBeInTheDocument();
  });

  it("shows a notice when the stations fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<MapPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load stations");
  });
});
