import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ImportResponse, SamplesResponse } from "@/api/generated";
import { TideGraph } from "./TideGraph";

const nextWeek: ImportResponse = {
  id: 1,
  locationId: 3,
  from: "2026-11-01T00:00:00+00:00",
  to: "2026-11-08T00:00:00+00:00",
  importedAt: "2026-10-30T12:00:00+00:00",
  entryCount: 1008,
};

const samples: SamplesResponse = {
  locationId: 3,
  timeZone: "UTC",
  unit: "cm",
  reference: "CD",
  intervalMinutes: 10,
  samples: [
    { time: "2026-11-01T00:00:00+00:00", levelCm: 248 },
    { time: "2026-11-01T00:10:00+00:00", levelCm: 245 },
  ],
};

function stubApi(imports: ImportResponse[]) {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) =>
      Promise.resolve(
        url.endsWith("/imports") ? Response.json({ imports }) : Response.json(samples),
      ),
    ),
  );
}

describe("TideGraph", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-11-01T10:00:00Z"));
  });

  afterEach(() => vi.useRealTimers());

  it("shows the water level for the imported days", async () => {
    stubApi([nextWeek]);

    render(<TideGraph locationId={3} />);

    expect(await screen.findByRole("figure", { name: "Water level" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "2 days", pressed: true })).toBeInTheDocument();
  });

  it("switches the days in view", async () => {
    const user = userEvent.setup();
    stubApi([nextWeek]);
    render(<TideGraph locationId={3} />);
    await screen.findByRole("figure", { name: "Water level" });

    await user.click(screen.getByRole("button", { name: "1 day" }));

    expect(screen.getByRole("button", { name: "1 day", pressed: true })).toBeInTheDocument();
    expect(screen.getByRole("figure", { name: "Water level" })).toBeInTheDocument();
  });

  it("says so when today isn't imported", async () => {
    stubApi([]);

    render(<TideGraph locationId={3} />);

    expect(await screen.findByText("No imported water levels for today.")).toBeInTheDocument();
  });

  it("shows an error when the water levels can't be loaded", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));

    render(<TideGraph locationId={3} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load water levels");
  });
});
