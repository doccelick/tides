import { useRef, useState } from "react";
import Map, { Layer, Source, Marker, Popup } from "react-map-gl/maplibre";
import { setWorkerUrl } from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";
import { useIsAdmin } from "@/auth/useIsAdmin";
import { Button } from "@/components/ui/button";
import { LocationImports } from "@/imports/LocationImports";
import { SamplesPanel } from "@/samples/SamplesPanel";
import { DeleteLocationButton } from "./DeleteLocationButton";
import { kartverketSource, type BaseLayer } from "./kartverketSource";
import { NewLocationCard } from "./NewLocationCard";
import { TideCard } from "./TideCard";
import { useLocations } from "./useLocations";
import { useLongPress, type PressPosition } from "./useLongPress";
import { useStations } from "./useStations";

// Bundlers need an explicit worker URL: https://maplibre.org/maplibre-gl-js/docs/#installation
setWorkerUrl(workerUrl);

// The card under the map: the tides at a station or location, or the form for a new location.
type Panel =
  | { kind: "tides"; name: string; path: string; locationId?: number }
  | { kind: "new-location"; position: PressPosition };

export function MapPage() {
  const [baseLayer, setBaseLayer] = useState<BaseLayer>("topo");
  const [panel, setPanel] = useState<Panel | null>(null);
  // Where a long press asks "Add location?".
  const [pressed, setPressed] = useState<PressPosition | null>(null);
  const selectedMarkerRef = useRef<HTMLButtonElement | null>(null);
  const isTopo = baseLayer === "topo";
  const isNautical = baseLayer === "nautical";
  const { stations, failed: stationsFailed } = useStations();
  const {
    locations,
    failed: locationsFailed,
    add: addLocation,
    remove: removeLocation,
  } = useLocations();
  const isAdmin = useIsAdmin();
  const longPress = useLongPress({
    onLongPress: (position) => {
      if (isAdmin) setPressed(position);
    },
    onClick: () => setPressed(null),
  });
  const [tidesVersion, setTidesVersion] = useState(0);

  function openTides(button: HTMLButtonElement, tides: Panel) {
    selectedMarkerRef.current = button;
    setPressed(null);
    setPanel(tides);
  }

  return (
    <div className="flex h-svh flex-col">
      <div className="relative min-h-0 flex-1">
        <Map
          {...longPress}
          mapStyle="https://tiles.openfreemap.org/styles/liberty"
          initialViewState={{
            bounds: [
              [4.5, 57.8],
              [31.5, 71.3],
            ],
            fitBoundsOptions: { padding: 16 },
          }}
          style={{ width: "100%", height: "100%" }}
          attributionControl={{ compact: false, customAttribution: "Not for navigation" }}
        >
          <Source id="kartverket" {...kartverketSource(baseLayer)}>
            <Layer id="kartverket" type="raster" source="kartverket" />
          </Source>
          {stations.map((station) => (
            <Marker key={station.code} latitude={station.latitude} longitude={station.longitude}>
              <button
                type="button"
                aria-label={station.name}
                onClick={(event) =>
                  openTides(event.currentTarget, {
                    kind: "tides",
                    name: station.name,
                    path: `/v1/stations/${station.code}`,
                  })
                }
                className="flex size-11 cursor-pointer items-center justify-center"
              >
                <span className="size-3 rounded-full bg-primary ring-2 ring-white" />
              </button>
            </Marker>
          ))}
          {locations.map((location) => (
            <Marker key={location.id} latitude={location.latitude} longitude={location.longitude}>
              <button
                type="button"
                aria-label={location.name}
                onClick={(event) =>
                  openTides(event.currentTarget, {
                    kind: "tides",
                    name: location.name,
                    path: `/v1/locations/${location.id}`,
                    locationId: location.id,
                  })
                }
                className="flex size-11 cursor-pointer items-center justify-center"
              >
                <span className="size-3 rounded-xs bg-chart-2 ring-2 ring-white" />
              </button>
            </Marker>
          ))}
          {panel?.kind === "new-location" && (
            <Marker latitude={panel.position.latitude} longitude={panel.position.longitude}>
              <span className="block size-3 rounded-xs border-2 border-chart-2 bg-white" />
            </Marker>
          )}
          {pressed && (
            <Popup
              latitude={pressed.latitude}
              longitude={pressed.longitude}
              anchor="bottom"
              closeButton={false}
              closeOnClick={false}
            >
              <Button
                className="h-11"
                autoFocus
                onKeyDown={(event) => {
                  if (event.key === "Escape") setPressed(null);
                }}
                onClick={() => {
                  setPanel({ kind: "new-location", position: pressed });
                  setPressed(null);
                }}
              >
                Add location?
              </Button>
            </Popup>
          )}
        </Map>
        <div className="absolute top-3 left-3 flex gap-2">
          <Button
            className="h-11"
            onClick={() => setBaseLayer("topo")}
            aria-pressed={isTopo}
            variant={isTopo ? "default" : "outline"}
          >
            Map
          </Button>
          <Button
            className="h-11"
            onClick={() => setBaseLayer("nautical")}
            aria-pressed={isNautical}
            variant={isNautical ? "default" : "outline"}
          >
            Nautical chart
          </Button>
        </div>
        <div className="absolute top-16 left-3 flex flex-col gap-2">
          {stationsFailed && (
            <p className="rounded-md bg-background/90 px-3 py-2 text-sm" role="alert">
              Couldn't load stations
            </p>
          )}
          {locationsFailed && (
            <p className="rounded-md bg-background/90 px-3 py-2 text-sm" role="alert">
              Couldn't load locations
            </p>
          )}
        </div>
      </div>
      {panel?.kind === "tides" && (
        <TideCard
          key={panel.path}
          name={panel.name}
          path={panel.path}
          tidesVersion={tidesVersion}
          onClose={() => {
            setPanel(null);
            selectedMarkerRef.current?.focus({ preventScroll: true });
          }}
        >
          {panel.locationId !== undefined && (
            <SamplesPanel key={tidesVersion} locationId={panel.locationId} />
          )}
          {isAdmin && panel.locationId !== undefined && (
            <>
              <LocationImports
                locationId={panel.locationId}
                onChanged={() => setTidesVersion((current) => current + 1)}
              />
              <DeleteLocationButton
                locationId={panel.locationId}
                name={panel.name}
                onDeleted={(locationId) => {
                  removeLocation(locationId);
                  setPanel(null);
                }}
              />
            </>
          )}
        </TideCard>
      )}
      {panel?.kind === "new-location" && (
        <NewLocationCard
          key={`${panel.position.latitude},${panel.position.longitude}`}
          position={panel.position}
          onLocationSaved={addLocation}
          onCreated={(location) => {
            setPanel({
              kind: "tides",
              name: location.name,
              path: `/v1/locations/${location.id}`,
              locationId: location.id,
            });
          }}
          onClose={() => setPanel(null)}
        />
      )}
    </div>
  );
}
