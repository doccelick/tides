import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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

const laterTides: TidesResponse = {
  ...tides,
  tides: [{ type: "High", time: "2026-10-22T04:10:00+00:00", levelCm: 231 }],
};

// Reports every observed element as in view, as if both ends of the strip were scrolled to.
class VisibleIntersectionObserver {
  callback: IntersectionObserverCallback;

  constructor(callback: IntersectionObserverCallback) {
    this.callback = callback;
  }

  observe(target: Element) {
    const entry = { target, isIntersecting: true } as IntersectionObserverEntry;
    this.callback([entry], this as unknown as IntersectionObserver);
  }

  disconnect() {}
}

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

  it("loads more days when an end of the strip comes into view", async () => {
    vi.stubGlobal("IntersectionObserver", VisibleIntersectionObserver);
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(Response.json(tides))
        .mockResolvedValueOnce(new Response(null, { status: 404 }))
        .mockResolvedValueOnce(Response.json(laterTides)),
    );
    render(<TidePanel code="BGO" />);

    expect(await screen.findByText("231 cm")).toBeInTheDocument();
  });

  it("retries loading more days after a failure", async () => {
    vi.stubGlobal("IntersectionObserver", VisibleIntersectionObserver);
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(Response.json(tides))
        .mockResolvedValueOnce(new Response(null, { status: 404 }))
        .mockResolvedValueOnce(new Response(null, { status: 502 }))
        .mockResolvedValueOnce(Response.json(laterTides)),
    );
    render(<TidePanel code="BGO" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load later tides");
    await userEvent.click(screen.getByRole("button", { name: "Retry" }));

    expect(await screen.findByText("231 cm")).toBeInTheDocument();
  });
});
