import { render, screen, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DeleteLocationButton } from "./DeleteLocationButton";

async function confirmDelete() {
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Delete location" }));
  await user.click(within(screen.getByRole("alertdialog")).getByRole("button", { name: "Delete" }));
}

describe("DeleteLocationButton", () => {
  it("deletes the location after confirming and reports it", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetch);
    const onDeleted = vi.fn();
    render(<DeleteLocationButton locationId={3} name="Kjerringøy" onDeleted={onDeleted} />);

    await confirmDelete();

    expect(fetch).toHaveBeenCalledWith(
      "/v1/locations/3",
      expect.objectContaining({ method: "DELETE" }),
    );
    expect(onDeleted).toHaveBeenCalledWith(3);
  });

  it("deletes nothing when cancelled", async () => {
    const user = userEvent.setup();
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    render(<DeleteLocationButton locationId={3} name="Kjerringøy" onDeleted={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Delete location" }));
    await user.click(
      within(screen.getByRole("alertdialog")).getByRole("button", { name: "Cancel" }),
    );

    expect(fetch).not.toHaveBeenCalled();
  });

  it("shows the API's message when the delete fails", async () => {
    const problem = { status: 404, title: "Not Found" };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(problem, { status: 404 })));
    const onDeleted = vi.fn();
    render(<DeleteLocationButton locationId={3} name="Kjerringøy" onDeleted={onDeleted} />);

    await confirmDelete();

    expect(await screen.findByRole("alert")).toHaveTextContent("Not Found");
    expect(onDeleted).not.toHaveBeenCalled();
  });
});
