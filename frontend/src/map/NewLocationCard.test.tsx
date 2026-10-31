import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ImportsResponse, LocationResponse } from "@/api/generated";
import { NewLocationCard } from "./NewLocationCard";

const position = { longitude: 14.76, latitude: 67.52 };
const location: LocationResponse = { id: 3, name: "Kjerringøy", ...position };
const imports: ImportsResponse = { imports: [] };

function problem(status: number, body: object): Response {
  return Response.json({ status, ...body }, { status });
}

async function submit(name: string) {
  const user = userEvent.setup();
  await user.type(screen.getByRole("textbox", { name: "Name" }), name);
  await user.click(screen.getByRole("button", { name: "Save and import" }));
  return user;
}

describe("NewLocationCard", () => {
  it("creates the location, imports the range and reports the new location", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(Response.json(location, { status: 201 }))
      .mockResolvedValueOnce(Response.json(imports, { status: 201 }));
    vi.stubGlobal("fetch", fetch);
    const onCreated = vi.fn();
    render(<NewLocationCard position={position} onCreated={onCreated} onClose={vi.fn()} />);

    await submit("Kjerringøy");

    expect(onCreated).toHaveBeenCalledWith(location);
    expect(fetch).toHaveBeenLastCalledWith("/v1/locations/3/imports", expect.anything());
  });

  it("shows the API's message and imports nothing when the location is refused", async () => {
    const fetch = vi.fn().mockResolvedValue(problem(422, { title: "Too far from the coast." }));
    vi.stubGlobal("fetch", fetch);
    const onCreated = vi.fn();
    render(<NewLocationCard position={position} onCreated={onCreated} onClose={vi.fn()} />);

    await submit("Inland");

    expect(await screen.findByRole("alert")).toHaveTextContent("Too far from the coast.");
    expect(fetch).toHaveBeenCalledOnce();
    expect(onCreated).not.toHaveBeenCalled();
  });

  it("shows the name's validation errors", async () => {
    const body = {
      title: "One or more validation errors occurred.",
      errors: { Name: ["The field Name must be a string with a maximum length of 100."] },
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(problem(400, body)));
    render(<NewLocationCard position={position} onCreated={vi.fn()} onClose={vi.fn()} />);

    await submit("Kjerringøy");

    expect(
      await screen.findByText("The field Name must be a string with a maximum length of 100."),
    ).toBeInTheDocument();
  });

  it("retries only the import when the location was saved but the import failed", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(Response.json(location, { status: 201 }))
      .mockResolvedValueOnce(problem(502, { title: "Kartverket's Tide API is unavailable." }))
      .mockResolvedValueOnce(Response.json(imports, { status: 201 }));
    vi.stubGlobal("fetch", fetch);
    const onCreated = vi.fn();
    render(<NewLocationCard position={position} onCreated={onCreated} onClose={vi.fn()} />);

    const user = await submit("Kjerringøy");

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Location saved, but the import failed: Kartverket's Tide API is unavailable.",
    );
    await user.click(screen.getByRole("button", { name: "Retry import" }));

    expect(onCreated).toHaveBeenCalledWith(location);
    expect(fetch).toHaveBeenCalledTimes(3);
    expect(fetch).toHaveBeenLastCalledWith("/v1/locations/3/imports", expect.anything());
  });
});
