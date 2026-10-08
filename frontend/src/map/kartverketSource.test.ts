import { describe, expect, it } from "vitest";
import { kartverketSource } from "./kartverketSource";

describe("kartverketSource", () => {
  it.each([
    ["topo", "topo"],
    ["nautical", "sjokartraster"],
  ] as const)("uses Kartverket's layer name for %s", (layer, kartverketName) => {
    const source = kartverketSource(layer);

    expect(source.tiles).toEqual([
      `https://cache.kartverket.no/v1/wmts/1.0.0/${kartverketName}/default/webmercator/{z}/{y}/{x}.png`,
    ]);
  });

  it("describes a Kartverket raster source", () => {
    const source = kartverketSource("topo");
    expect(source.type).toBe("raster");
    expect(source.attribution).toBe("© Kartverket");
    expect(source.maxzoom).toBe(18);
  });
});
