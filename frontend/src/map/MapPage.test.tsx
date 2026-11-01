import { act, render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import type { MapLayerMouseEvent } from "react-map-gl/maplibre";
import { afterEach, describe, expect, it, vi, beforeEach } from "vitest";
import { MapPage } from "./MapPage";
import type {
  ImportsResponse,
  LocationResponse,
  StationResponse,
  TidesResponse,
} from "@/api/generated";

// The props the page last passed to the map, so tests can send map events.
const map = vi.hoisted(() => ({ props: {} as Record<string, unknown> }));

vi.mock("react-map-gl/maplibre", () => ({
  default: (props: {
    children: React.ReactNode;
    attributionControl: { customAttribution: string };
  }) => {
    map.props = props;
    return (
      <div data-testid="map" data-attribution={props.attributionControl.customAttribution}>
        {props.children}
      </div>
    );
  },
  Marker: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Popup: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Source: ({ tiles }: { tiles: string[] }) => (
    <div data-testid="kartverket-source" data-tiles={tiles[0]} />
  ),
  Layer: () => null,
}));

const auth = vi.hoisted(() => ({ isAdmin: true }));

vi.mock("@/auth/useIsAdmin", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@/auth/useIsAdmin")>()),
  useIsAdmin: () => auth.isAdmin,
}));

const stations: StationResponse[] = [
  { code: "BGO", name: "Bergen", latitude: 60.4, longitude: 5.3 },
  { code: "OSL", name: "Oslo", latitude: 59.9, longitude: 10.7 },
];

const locations: LocationResponse[] = [
  { id: 3, name: "Kjerringøy", latitude: 67.52, longitude: 14.76 },
];

const tides: TidesResponse = {
  timeZone: "UTC",
  unit: "cm",
  reference: "CD",
  source: "Kartverket",
  tides: [],
};

describe("MapPage", () => {
  beforeEach(() => {
    auth.isAdmin = true;
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.includes("/tides?")) return Promise.resolve(Response.json(tides));
        if (url.endsWith("/water-level"))
          return Promise.resolve(new Response(null, { status: 404 }));
        if (url === "/v1/locations") return Promise.resolve(Response.json({ locations }));
        if (url.endsWith("/imports")) return Promise.resolve(Response.json({ imports: [] }));
        return Promise.resolve(Response.json({ stations }));
      }),
    );
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
    expect(screen.queryByRole("region", { name: "Imports" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Graph" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("region", { name: "Bergen" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Bergen" })).toHaveFocus();
  });

  it("opens a location's card with the location's tides", async () => {
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(await screen.findByRole("button", { name: "Kjerringøy" }));

    expect(screen.getByRole("region", { name: "Kjerringøy" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Imports" })).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining("/v1/locations/3/tides?"),
      expect.anything(),
    );
  });

  it("leaves the imports and delete out of a location's card when not an admin", async () => {
    auth.isAdmin = false;
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(await screen.findByRole("button", { name: "Kjerringøy" }));

    expect(screen.getByRole("region", { name: "Kjerringøy" })).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Imports" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete location" })).not.toBeInTheDocument();
  });

  it("switches a location's card between the tides and the graph", async () => {
    auth.isAdmin = false;
    const user = userEvent.setup();
    render(<MapPage />);
    await user.click(await screen.findByRole("button", { name: "Kjerringøy" }));

    await user.click(screen.getByRole("button", { name: "Graph" }));

    expect(await screen.findByText("No imported water levels for today.")).toBeInTheDocument();
    expect(screen.queryByRole("list", { name: "Tides" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "2 days", pressed: true })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Tides" }));

    expect(await screen.findByRole("list", { name: "Tides" })).toBeInTheDocument();
    expect(screen.queryByText("No imported water levels for today.")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "2 days" })).not.toBeInTheDocument();
  });

  it("replaces an open station card with the location's card", async () => {
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(await screen.findByRole("button", { name: "Bergen" }));
    await user.click(await screen.findByRole("button", { name: "Kjerringøy" }));

    expect(screen.queryByRole("region", { name: "Bergen" })).not.toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Kjerringøy" })).toBeInTheDocument();
  });

  describe("adding a location", () => {
    const created: LocationResponse = { id: 4, name: "Grøtøy", latitude: 67.8, longitude: 14.7 };
    const imports: ImportsResponse = { imports: [] };
    const press = {
      point: { x: 100, y: 100 },
      lngLat: { lng: 14.7, lat: 67.8 },
      originalEvent: { button: 0 },
    } as unknown as MapLayerMouseEvent;

    beforeEach(() => {
      vi.useFakeTimers({ shouldAdvanceTime: true });
      vi.stubGlobal(
        "fetch",
        vi.fn((url: string, init?: RequestInit) => {
          if (init?.method === "POST" && url === "/v1/locations")
            return Promise.resolve(Response.json(created, { status: 201 }));
          if (init?.method === "POST")
            return Promise.resolve(Response.json(imports, { status: 201 }));
          if (url.includes("/tides?")) return Promise.resolve(Response.json(tides));
          if (url.endsWith("/water-level"))
            return Promise.resolve(new Response(null, { status: 404 }));
          if (url === "/v1/locations") return Promise.resolve(Response.json({ locations: [] }));
          if (url.endsWith("/imports")) return Promise.resolve(Response.json(imports));
          return Promise.resolve(Response.json({ stations }));
        }),
      );
    });

    afterEach(() => vi.useRealTimers());

    function longPress() {
      act(() => (map.props.onMouseDown as (event: MapLayerMouseEvent) => void)(press));
      act(() => vi.advanceTimersByTime(500));
    }

    it("saves a location from a long press and opens its card", async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      render(<MapPage />);

      longPress();
      await user.click(screen.getByRole("button", { name: "Add location?" }));
      await user.type(screen.getByRole("textbox", { name: "Name" }), "Grøtøy");
      await user.click(screen.getByRole("button", { name: "Save and import" }));

      expect(await screen.findByRole("region", { name: "Grøtøy" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Grøtøy" })).toBeInTheDocument();
    });

    it("ignores a long press when not an admin", () => {
      auth.isAdmin = false;
      render(<MapPage />);

      longPress();

      expect(screen.queryByRole("button", { name: "Add location?" })).not.toBeInTheDocument();
    });

    it("closes the popup on a map click", async () => {
      render(<MapPage />);

      longPress();
      expect(screen.getByRole("button", { name: "Add location?" })).toBeInTheDocument();

      act(() => (map.props.onMouseDown as (event: MapLayerMouseEvent) => void)(press));
      act(() => (map.props.onMouseUp as () => void)());
      act(() => (map.props.onClick as () => void)());

      expect(screen.queryByRole("button", { name: "Add location?" })).not.toBeInTheDocument();
    });
  });

  it("shows a notice when the stations fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<MapPage />);

    expect(await screen.findByText("Couldn't load stations")).toHaveAttribute("role", "alert");
  });

  it("shows a notice when the locations fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<MapPage />);

    expect(await screen.findByText("Couldn't load locations")).toHaveAttribute("role", "alert");
  });
});
