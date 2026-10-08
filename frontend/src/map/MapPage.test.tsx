import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MapPage } from "./MapPage";

vi.mock("react-map-gl/maplibre", () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Source: ({ tiles }: { tiles: string[] }) => (
    <div data-testid="kartverket-source" data-tiles={tiles[0]} />
  ),
  Layer: () => null,
}));

describe("MapPage", () => {
  it("starts on the topo map", () => {
    render(<MapPage />);

    expect(screen.getByRole("button", { name: "Map", pressed: true })).toBeInTheDocument();
    expect(screen.getByTestId("kartverket-source").getAttribute("data-tiles")).toContain("/topo/");
  });

  it("switches to the nautical chart", async () => {
    const user = userEvent.setup();
    render(<MapPage />);

    await user.click(screen.getByRole("button", { name: "Nautical chart" }));

    expect(
      screen.getByRole("button", { name: "Nautical chart", pressed: true }),
    ).toBeInTheDocument();
    expect(screen.getByTestId("kartverket-source").getAttribute("data-tiles")).toContain(
      "/sjokartraster/",
    );
  });

  it("shows the not-for-navigation note", () => {
    render(<MapPage />);

    expect(screen.getByText("Not for navigation")).toBeInTheDocument();
  });
});
