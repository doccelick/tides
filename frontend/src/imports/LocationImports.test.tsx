import { render, screen, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ImportResponse } from "@/api/generated";
import { LocationImports } from "./LocationImports";

const november: ImportResponse = {
  id: 12,
  locationId: 3,
  from: "2026-11-01T00:00:00+00:00",
  to: "2026-11-08T00:00:00+00:00",
  importedAt: "2026-10-30T18:02:11+00:00",
  entryCount: 1031,
};

const december: ImportResponse = {
  ...november,
  id: 13,
  from: "2026-12-01T00:00:00+00:00",
  to: "2026-12-08T00:00:00+00:00",
};

describe("LocationImports", () => {
  let stored: ImportResponse[];

  beforeEach(() => {
    stored = [november];
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string, init?: RequestInit) => {
        if (init?.method === "POST") {
          stored = [...stored, december];
          return Promise.resolve(Response.json({ imports: [december] }, { status: 201 }));
        }
        if (init?.method === "DELETE") {
          stored = stored.filter((i) => url !== `/v1/imports/${i.id}`);
          return Promise.resolve(new Response(null, { status: 204 }));
        }
        return Promise.resolve(Response.json({ imports: stored }));
      }),
    );
  });

  it("lists the location's imports with their entry counts", async () => {
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    expect(await screen.findByText("1 Nov – 7 Nov 2026")).toBeInTheDocument();
    expect(screen.getByText("1,031 entries")).toBeInTheDocument();
  });

  it("starts collapsed with the number of imports and opens on tap", async () => {
    const user = userEvent.setup();
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    const summary = await screen.findByText("Imports (1)");
    expect(summary.closest("details")).not.toHaveAttribute("open");

    await user.click(summary);

    expect(summary.closest("details")).toHaveAttribute("open");
  });

  it("deletes an import after confirming and reports the change", async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    render(<LocationImports locationId={3} onChanged={onChanged} />);

    await user.click(await screen.findByRole("button", { name: "Delete 1 Nov – 7 Nov 2026" }));
    await user.click(
      within(screen.getByRole("alertdialog")).getByRole("button", { name: "Delete" }),
    );

    expect(await screen.findByText("No imports yet.")).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledWith(
      "/v1/imports/12",
      expect.objectContaining({ method: "DELETE" }),
    );
    expect(onChanged).toHaveBeenCalledOnce();
  });

  it("keeps the import when the delete is cancelled", async () => {
    const user = userEvent.setup();
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    await user.click(await screen.findByRole("button", { name: "Delete 1 Nov – 7 Nov 2026" }));
    await user.click(
      within(screen.getByRole("alertdialog")).getByRole("button", { name: "Cancel" }),
    );

    expect(screen.getByText("1 Nov – 7 Nov 2026")).toBeInTheDocument();
    expect(fetch).not.toHaveBeenCalledWith(
      "/v1/imports/12",
      expect.objectContaining({ method: "DELETE" }),
    );
  });

  it("imports the chosen range and shows which days were added", async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    render(<LocationImports locationId={3} onChanged={onChanged} />);

    await user.click(await screen.findByRole("button", { name: "Import" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Imported 1 Dec – 7 Dec 2026");
    expect(await screen.findByText("1 Dec – 7 Dec 2026")).toBeInTheDocument();
    expect(onChanged).toHaveBeenCalledOnce();
  });

  it("shows the API's message when the import fails", async () => {
    const user = userEvent.setup();
    const title = "Kartverket's Tide API is unavailable.";
    vi.stubGlobal(
      "fetch",
      vi.fn((_url: string, init?: RequestInit) =>
        Promise.resolve(
          init?.method === "POST"
            ? Response.json({ status: 502, title }, { status: 502 })
            : Response.json({ imports: stored }),
        ),
      ),
    );
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    await user.click(await screen.findByRole("button", { name: "Import" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(title);
  });

  describe("when the chosen range is already imported", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["Date"] });
      vi.setSystemTime(new Date("2026-10-31T10:00:00Z"));
      // Covers "Next 31 days", the default preset, from 31 October.
      stored = [
        { ...november, from: "2026-10-31T00:00:00+00:00", to: "2026-12-01T00:00:00+00:00" },
      ];
    });

    afterEach(() => vi.useRealTimers());

    it("disables the import", async () => {
      render(<LocationImports locationId={3} onChanged={vi.fn()} />);

      expect(await screen.findByRole("button", { name: "Already imported" })).toBeDisabled();
    });
  });

  it("shows a notice when the imports fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load imports");
  });
});
