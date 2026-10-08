import { useState } from "react";
import Map, { Layer, Source, Marker } from "react-map-gl/maplibre";
import { setWorkerUrl } from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";
import type { StationResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { kartverketSource, type BaseLayer } from "./kartverketSource";
import { StationCard } from "./StationCard";
import { useStations } from "./useStations";

// Bundlers need an explicit worker URL: https://maplibre.org/maplibre-gl-js/docs/#installation
setWorkerUrl(workerUrl);

export function MapPage() {
  const [baseLayer, setBaseLayer] = useState<BaseLayer>("topo");
  const [selectedStation, setSelectedStation] = useState<StationResponse | null>(null);
  const isTopo = baseLayer === "topo";
  const isNautical = baseLayer === "nautical";
  const { stations, failed } = useStations();

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
                onClick={() => setSelectedStation(station)}
                className="flex size-11 cursor-pointer items-center justify-center"
              >
                <span className="size-3 rounded-full bg-primary ring-2 ring-white" />
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
        {failed && (
          <p
            className="absolute top-16 left-3 rounded-md bg-background/90 px-3 py-2 text-sm"
            role="alert"
          >
            Couldn't load stations
          </p>
        )}
      </div>
      {selectedStation && (
        <StationCard station={selectedStation} onClose={() => setSelectedStation(null)} />
      )}
    </div>
  );
}
