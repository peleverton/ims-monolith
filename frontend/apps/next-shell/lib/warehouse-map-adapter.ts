/**
 * Epic 5: Adapter Pattern — converts backend route graph nodes
 * into screen coordinates for SVG/Canvas rendering.
 */

export interface RouteStep {
  order: number;
  locationId: string;
  locationName: string;
  locationCode: string;
  row: number;
  col: number;
  instruction: string;
  distanceFromPrevious: number;
}

export interface RouteResult {
  steps: RouteStep[];
  totalDistance: number;
  strategy: string;
  computationTime: string;
  visitOrder: string[];
}

export interface WarehouseNodeDto {
  locationId: string;
  name: string;
  code: string;
  row: number;
  col: number;
  isWalkable: boolean;
  type: string;
}

export interface WarehouseLayoutDto {
  rows: number;
  cols: number;
  nodes: WarehouseNodeDto[];
}

// ─── Adapter: Graph → Screen Coordinates ──────────────────────────────────────

export interface ScreenNode {
  id: string;
  name: string;
  code: string;
  x: number;
  y: number;
  row: number;
  col: number;
  isWalkable: boolean;
  isTarget: boolean;
  isOnRoute: boolean;
  routeOrder?: number;
}

export interface ScreenEdge {
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  order: number;
}

export interface WarehouseMapData {
  nodes: ScreenNode[];
  edges: ScreenEdge[];
  width: number;
  height: number;
}

const CELL_SIZE = 64;
const PADDING = 32;

/**
 * Adapter: Converts warehouse layout + route into screen-ready data.
 */
export function adaptLayoutToScreen(
  layout: WarehouseLayoutDto,
  route?: RouteResult,
  targetIds?: string[]
): WarehouseMapData {
  const routeNodeIds = new Set(route?.steps.map((s) => s.locationId) ?? []);
  const targetSet = new Set(targetIds ?? []);

  // Build route order map
  const routeOrderMap = new Map<string, number>();
  route?.steps.forEach((s) => routeOrderMap.set(s.locationId, s.order));

  const nodes: ScreenNode[] = layout.nodes.map((node) => ({
    id: node.locationId,
    name: node.name,
    code: node.code,
    x: PADDING + node.col * CELL_SIZE + CELL_SIZE / 2,
    y: PADDING + node.row * CELL_SIZE + CELL_SIZE / 2,
    row: node.row,
    col: node.col,
    isWalkable: node.isWalkable,
    isTarget: targetSet.has(node.locationId),
    isOnRoute: routeNodeIds.has(node.locationId),
    routeOrder: routeOrderMap.get(node.locationId),
  }));

  // Build edges from route steps (sequential connections)
  const edges: ScreenEdge[] = [];
  if (route && route.steps.length > 1) {
    const nodeMap = new Map(nodes.map((n) => [n.id, n]));
    for (let i = 1; i < route.steps.length; i++) {
      const from = nodeMap.get(route.steps[i - 1].locationId);
      const to = nodeMap.get(route.steps[i].locationId);
      if (from && to) {
        edges.push({
          fromX: from.x,
          fromY: from.y,
          toX: to.x,
          toY: to.y,
          order: i,
        });
      }
    }
  }

  return {
    nodes,
    edges,
    width: PADDING * 2 + layout.cols * CELL_SIZE,
    height: PADDING * 2 + layout.rows * CELL_SIZE,
  };
}
