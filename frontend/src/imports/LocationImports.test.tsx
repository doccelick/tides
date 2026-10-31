import { render, screen, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
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

describe("LocationImports", () => {
  let stored: ImportResponse[];

  beforeEach(() => {
    stored = [november];
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string, init?: RequestInit) => {
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

  it("deletes an import after confirming and reports the change", async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    render(<LocationImports locationId={3} onChanged={onChanged} />);

    await user.click(await screen.findByRole("button", { name: "Delete 1 Nov – 7 Nov 2026" }));
    await user.click(
      within(screen.getByRole("alertdialog")).getByRole("button", { name: "Delete" }),
    );

    expect(await screen.findByText("No imports yet.")).toBeInTheDocument();
    expect(fetch).toHaveBeenCalledWith("/v1/imports/12", { method: "DELETE" });
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
    expect(fetch).not.toHaveBeenCalledWith("/v1/imports/12", { method: "DELETE" });
  });

  it("shows a notice when the imports fail to load", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 502 })));
    render(<LocationImports locationId={3} onChanged={vi.fn()} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Couldn't load imports");
  });
});
