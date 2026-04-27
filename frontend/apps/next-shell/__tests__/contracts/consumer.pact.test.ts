/**
 * US-073: Pact Consumer Tests — BFF (Next.js) ↔ API (.NET)
 *
 * Tests define the contract for the critical endpoints:
 *  - POST /api/auth/login
 *  - GET  /api/issues
 *  - GET  /api/inventory/products
 *
 * Pact files are written to pacts/ directory and versioned in the repo.
 */

import path from "path";
import { PactV3, MatchersV3 } from "@pact-foundation/pact";
import { describe, it, expect, beforeAll, afterAll } from "vitest";

const { like, string, eachLike, integer } = MatchersV3;

const provider = new PactV3({
  consumer: "IMS-BFF-NextJS",
  provider: "IMS-API-DotNet",
  dir: path.resolve(process.cwd(), "pacts"),
  logLevel: "warn",
});

describe("Pact Consumer Tests — IMS BFF ↔ API", () => {
  // ─── POST /api/auth/login ───────────────────────────────────
  describe("POST /api/auth/login", () => {
    it("returns access token on valid credentials", async () => {
      await provider
        .given("admin user exists")
        .uponReceiving("a login request with valid credentials")
        .withRequest({
          method: "POST",
          path: "/api/auth/login",
          headers: { "Content-Type": "application/json" },
          body: { username: "admin", password: like("Admin@123!") },
        })
        .willRespondWith({
          status: 200,
          headers: { "Content-Type": "application/json; charset=utf-8" },
          body: {
            accessToken: string("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.example"),
            refreshToken: string("refresh-token-hash"),
            username: string("admin"),
            email: string("admin@ims.com"),
            roles: eachLike("Admin"),
          },
        })
        .executeTest(async (mockServer) => {
          const res = await fetch(`${mockServer.url}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: "admin", password: "Admin@123!" }),
          });
          const data = await res.json();
          expect(res.status).toBe(200);
          expect(data).toHaveProperty("accessToken");
          expect(data).toHaveProperty("refreshToken");
          expect(data.username).toBe("admin");
        });
    });
  });

  // ─── GET /api/issues ───────────────────────────────────────
  describe("GET /api/issues", () => {
    it("returns paginated list of issues", async () => {
      await provider
        .given("issues exist")
        .uponReceiving("a request for issues list")
        .withRequest({
          method: "GET",
          path: "/api/issues",
          headers: { Authorization: like("Bearer token") },
        })
        .willRespondWith({
          status: 200,
          headers: { "Content-Type": "application/json; charset=utf-8" },
          body: {
            items: eachLike({
              id: string("00000000-0000-0000-0000-000000000001"),
              title: string("Sample Issue"),
              status: string("Open"),
              priority: string("Medium"),
            }),
            totalCount: integer(1),
            page: integer(1),
            pageSize: integer(20),
          },
        })
        .executeTest(async (mockServer) => {
          const res = await fetch(`${mockServer.url}/api/issues`, {
            headers: { Authorization: "Bearer token" },
          });
          const data = await res.json();
          expect(res.status).toBe(200);
          expect(data).toHaveProperty("items");
          expect(Array.isArray(data.items)).toBe(true);
        });
    });
  });

  // ─── GET /api/inventory/products ──────────────────────────
  describe("GET /api/inventory/products", () => {
    it("returns paginated list of products", async () => {
      await provider
        .given("products exist")
        .uponReceiving("a request for inventory products")
        .withRequest({
          method: "GET",
          path: "/api/inventory/products",
          headers: { Authorization: like("Bearer token") },
        })
        .willRespondWith({
          status: 200,
          headers: { "Content-Type": "application/json; charset=utf-8" },
          body: {
            items: eachLike({
              id: string("00000000-0000-0000-0000-000000000001"),
              name: string("Laptop Dell"),
              sku: string("DELL-001"),
              quantityInStock: integer(10),
            }),
            totalCount: integer(1),
          },
        })
        .executeTest(async (mockServer) => {
          const res = await fetch(`${mockServer.url}/api/inventory/products`, {
            headers: { Authorization: "Bearer token" },
          });
          const data = await res.json();
          expect(res.status).toBe(200);
          expect(data).toHaveProperty("items");
        });
    });
  });
});
