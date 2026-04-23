/**
 * US-058: Unit tests for lib/utils.ts (cn helper).
 */

import { describe, it, expect } from "vitest";
import { cn } from "@/lib/utils";

describe("cn (classname merger)", () => {
  it("returns a single class as-is", () => {
    expect(cn("text-blue-500")).toBe("text-blue-500");
  });

  it("merges multiple classes", () => {
    expect(cn("px-4", "py-2")).toBe("px-4 py-2");
  });

  it("resolves Tailwind conflicts — later class wins", () => {
    // twMerge: px-4 followed by px-6 → px-6
    expect(cn("px-4", "px-6")).toBe("px-6");
  });

  it("ignores falsy values", () => {
    expect(cn("text-red-500", false, null, undefined, "font-bold")).toBe(
      "text-red-500 font-bold"
    );
  });

  it("handles conditional classes via object syntax", () => {
    expect(cn({ "bg-blue-500": true, "bg-red-500": false })).toBe("bg-blue-500");
  });

  it("handles array syntax", () => {
    expect(cn(["flex", "items-center"])).toBe("flex items-center");
  });

  it("returns empty string for no arguments", () => {
    expect(cn()).toBe("");
  });

  it("returns empty string for all-falsy arguments", () => {
    expect(cn(false, null, undefined)).toBe("");
  });

  it("merges complex Tailwind conflicts correctly", () => {
    // bg-blue followed by bg-red — twMerge keeps bg-red
    expect(cn("bg-blue-500 text-sm", "bg-red-500")).toBe("text-sm bg-red-500");
  });
});
