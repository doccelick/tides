import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { SamplesResponse } from "@/api/generated";
import { SamplesPanel } from "./SamplesPanel";

function samplesResponse(): Response {
  const body: SamplesResponse = {
    locationId: 3,
    timeZone: "UTC",
    unit: "cm",
    reference: "CD",
    intervalMinutes: 10,
    samples: [
      { time: "2026-10-31T00:00:00+00:00", levelCm: 248 },
      { time: "2026-10-31T00:10:00+00:00", levelCm: 245 },
    ],
  };
  return Response.json(body);
}

describe("SamplesPanel", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-10-31T10:00:00Z"));
  });

  afterEach(() => vi.useRealTimers());

  it("shows the next 7 days every 10 minutes first", async () => {
    const fetch = vi.fn().mockResolvedValue(samplesResponse());
    vi.stubGlobal("fetch", fetch);

    render(<SamplesPanel locationId={3} />);

    expect(await screen.findByRole("figure", { name: "Water level" })).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledWith(
      "/v1/locations/3/samples?from=2026-10-31&days=7&interval=10",
      expect.anything(),
    );
  });

  it("fetches every 60 minutes when that interval is picked", async () => {
    const user = userEvent.setup();
    const fetch = vi.fn().mockImplementation(() => Promise.resolve(samplesResponse()));
    vi.stubGlobal("fetch", fetch);
    render(<SamplesPanel locationId={3} />);
    await screen.findByRole("figure", { name: "Water level" });

    await user.click(screen.getByRole("button", { name: "60 min" }));

    expect(screen.getByRole("button", { name: "60 min" })).toHaveAttribute("aria-pressed", "true");
    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-10-31&days=7&interval=60",
      expect.anything(),
    );
  });

  it("fetches the range that is picked", async () => {
    const user = userEvent.setup();
    const fetch = vi.fn().mockImplementation(() => Promise.resolve(samplesResponse()));
    vi.stubGlobal("fetch", fetch);
    render(<SamplesPanel locationId={3} />);
    await screen.findByRole("figure", { name: "Water level" });

    await user.click(screen.getByRole("combobox", { name: "Chart range" }));
    await user.click(await screen.findByRole("option", { name: "Next month" }));

    expect(fetch).toHaveBeenLastCalledWith(
      "/v1/locations/3/samples?from=2026-11-01&days=30&interval=10",
      expect.anything(),
    );
  });

  it("says so when the range isn't imported", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));

    render(<SamplesPanel locationId={3} />);

    expect(await screen.findByText("Not imported for this range.")).toBeInTheDocument();
  });

  it("shows an error when the samples can't be loaded", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    render(<SamplesPanel locationId={3} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load water levels");
  });
});
