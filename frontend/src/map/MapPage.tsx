import { useRef, useState } from "react";
import Map, { Layer, Source, Marker } from "react-map-gl/maplibre";
import { setWorkerUrl } from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";
import { Button } from "@/components/ui/button";
import { kartverketSource, type BaseLayer } from "./kartverketSource";
import { TideCard } from "./TideCard";
import { useLocations } from "./useLocations";
import { useStations } from "./useStations";

// Bundlers need an explicit worker URL: https://maplibre.org/maplibre-gl-js/docs/#installation
setWorkerUrl(workerUrl);

interface Selected {
  name: string;
  path: string;
}

export function MapPage() {
  const [baseLayer, setBaseLayer] = useState<BaseLayer>("topo");
  const [selected, setSelected] = useState<Selected | null>(null);
  const selectedMarkerRef = useRef<HTMLButtonElement | null>(null);
  const isTopo = baseLayer === "topo";
  const isNautical = baseLayer === "nautical";
  const { stations, failed: stationsFailed } = useStations();
  const { locations, failed: locationsFailed } = useLocations();

  return (
    <div className="flex h-svh flex-col">
      <div className="relative min-h-0 flex-1">
        <Map
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
                onClick={(event) => {
                  selectedMarkerRef.current = event.currentTarget;
                  setSelected({ name: station.name, path: `/v1/stations/${station.code}` });
                }}
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
                onClick={(event) => {
                  selectedMarkerRef.current = event.currentTarget;
                  setSelected({ name: location.name, path: `/v1/locations/${location.id}` });
                }}
                className="flex size-11 cursor-pointer items-center justify-center"
              >
                <span className="size-3 rounded-xs bg-chart-2 ring-2 ring-white" />
              </button>
            </Marker>
          ))}
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
      {selected && (
        <TideCard
          key={selected.path}
          name={selected.name}
          path={selected.path}
          onClose={() => {
            setSelected(null);
            selectedMarkerRef.current?.focus({ preventScroll: true });
          }}
        />
      )}
    </div>
  );
}
