import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, beforeEach, vi } from "vitest";

// jsdom has no IntersectionObserver. Tests that need one to report visibility stub their own.
class NoopIntersectionObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

beforeEach(() => vi.stubGlobal("IntersectionObserver", NoopIntersectionObserver));

afterEach(() => cleanup());
