import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

/**
 * Provides the browser ResizeObserver API required by React Flow in jsdom tests.
 */
class TestResizeObserver {
  /**
   * Starts observing an element size during component tests.
   */
  observe() {
    return undefined;
  }

  /**
   * Stops observing one element during component tests.
   */
  unobserve() {
    return undefined;
  }

  /**
   * Stops all active observations during component tests.
   */
  disconnect() {
    return undefined;
  }
}

globalThis.ResizeObserver ??= TestResizeObserver;

afterEach(() => {
  cleanup();
});
