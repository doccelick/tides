import Map, { Layer, Source } from "react-map-gl/maplibre";
import "maplibre-gl/dist/maplibre-gl.css";
import { kartverketSource } from "./kartverketSource";

export function MapPage() {
  return (
    <Map
      mapStyle="https://tiles.openfreemap.org/styles/liberty"
      initialViewState={{ longitude: 15, latitude: 65, zoom: 4 }}
      style={{ width: "100%", height: "100svh" }}
    >
      <Source id="kartverket" {...kartverketSource("topo")}>
        <Layer id="kartverket" type="raster" source="kartverket" />
      </Source>
    </Map>
  );
}
