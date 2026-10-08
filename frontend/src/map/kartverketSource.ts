import type { RasterSourceSpecification } from "maplibre-gl";

export type BaseLayer = "topo" | "nautical";

const kartverketLayerNames: Record<BaseLayer, string> = {
  topo: "topo",
  nautical: "sjokartraster",
};

export function kartverketSource(layer: BaseLayer): RasterSourceSpecification {
  const url = `https://cache.kartverket.no/v1/wmts/1.0.0/${kartverketLayerNames[layer]}/default/webmercator/{z}/{y}/{x}.png`;
  return { type: "raster", tiles: [url], tileSize: 256, maxzoom: 18, attribution: "© Kartverket" };
}
