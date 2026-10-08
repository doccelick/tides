import { useState } from "react";
import Map, { Layer, Source } from "react-map-gl/maplibre";
import "maplibre-gl/dist/maplibre-gl.css";
import { kartverketSource, type BaseLayer } from "./kartverketSource";
import { Button } from "@/components/ui/button";
import { setWorkerUrl } from "maplibre-gl";
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";

// Bundlers need an explicit worker URL: https://maplibre.org/maplibre-gl-js/docs/#installation
setWorkerUrl(workerUrl);

export function MapPage() {
  const [baseLayer, setBaseLayer] = useState<BaseLayer>("topo");
  const isTopo = baseLayer === "topo";
  const isNautical = baseLayer === "nautical";

  return (
    <div className="relative">
      <Map
        mapStyle="https://tiles.openfreemap.org/styles/liberty"
        initialViewState={{ longitude: 15, latitude: 65, zoom: 4 }}
        style={{ width: "100%", height: "100svh" }}
        attributionControl={{ compact: false }}
      >
        <Source id="kartverket" {...kartverketSource(baseLayer)}>
          <Layer id="kartverket" type="raster" source="kartverket" />
        </Source>
      </Map>
      <p className="absolute bottom-3 left-3 rounded-md bg-background/80 px-2 py-1 text-xs">
        Not for navigation
      </p>
      <div className="absolute top-3 left-3 flex gap-2">
        <Button
          onClick={() => setBaseLayer("topo")}
          aria-pressed={isTopo}
          variant={isTopo ? "default" : "outline"}
        >
          Map
        </Button>
        <Button
          onClick={() => setBaseLayer("nautical")}
          aria-pressed={isNautical}
          variant={isNautical ? "default" : "outline"}
        >
          Nautical chart
        </Button>
      </div>
    </div>
  );
}
