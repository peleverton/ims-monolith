"use client";

import { type ScreenNode, type ScreenEdge } from "@/lib/warehouse-map-adapter";

/**
 * Epic 5: Presentational Component — renders the warehouse grid as SVG.
 * Pure display component (no data fetching or logic).
 */

interface WarehouseGridProps {
  nodes: ScreenNode[];
  edges: ScreenEdge[];
  width: number;
  height: number;
  animatedEdgeIndex: number;
}

export function WarehouseGrid({
  nodes,
  edges,
  width,
  height,
  animatedEdgeIndex,
}: WarehouseGridProps) {
  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      className="w-full h-auto border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-900"
      style={{ maxHeight: "600px" }}
    >
      {/* Grid cells background */}
      {nodes.map((node) => (
        <rect
          key={node.id}
          x={node.x - 28}
          y={node.y - 28}
          width={56}
          height={56}
          rx={6}
          className={getCellClass(node)}
          stroke={getCellStroke(node)}
          strokeWidth={node.isTarget ? 2.5 : 1}
        />
      ))}

      {/* Route edges (animated path) */}
      {edges.map((edge, i) => (
        <line
          key={`edge-${i}`}
          x1={edge.fromX}
          y1={edge.fromY}
          x2={edge.toX}
          y2={edge.toY}
          stroke={i <= animatedEdgeIndex ? "#3b82f6" : "#d1d5db"}
          strokeWidth={i <= animatedEdgeIndex ? 3 : 1.5}
          strokeDasharray={i > animatedEdgeIndex ? "4 4" : "none"}
          strokeLinecap="round"
          className="transition-all duration-500"
        />
      ))}

      {/* Route direction arrows */}
      {edges
        .filter((_, i) => i <= animatedEdgeIndex)
        .map((edge, i) => {
          const midX = (edge.fromX + edge.toX) / 2;
          const midY = (edge.fromY + edge.toY) / 2;
          const angle =
            Math.atan2(edge.toY - edge.fromY, edge.toX - edge.fromX) *
            (180 / Math.PI);
          return (
            <polygon
              key={`arrow-${i}`}
              points="-5,-4 5,0 -5,4"
              transform={`translate(${midX},${midY}) rotate(${angle})`}
              fill="#3b82f6"
            />
          );
        })}

      {/* Node labels */}
      {nodes.map((node) => (
        <g key={`label-${node.id}`}>
          {/* Route order badge */}
          {node.routeOrder !== undefined && (
            <circle
              cx={node.x + 20}
              cy={node.y - 20}
              r={10}
              fill="#3b82f6"
              stroke="white"
              strokeWidth={2}
            />
          )}
          {node.routeOrder !== undefined && (
            <text
              x={node.x + 20}
              y={node.y - 16}
              textAnchor="middle"
              className="text-[10px] fill-white font-bold"
            >
              {node.routeOrder}
            </text>
          )}

          {/* Code label */}
          <text
            x={node.x}
            y={node.y + 4}
            textAnchor="middle"
            className={`text-[11px] font-medium ${
              node.isTarget
                ? "fill-blue-700 dark:fill-blue-300"
                : "fill-gray-600 dark:fill-gray-400"
            }`}
          >
            {node.code}
          </text>
        </g>
      ))}
    </svg>
  );
}

function getCellClass(node: ScreenNode): string {
  if (!node.isWalkable) return "fill-gray-300 dark:fill-gray-600";
  if (node.isTarget) return "fill-blue-100 dark:fill-blue-900/40";
  if (node.isOnRoute) return "fill-green-50 dark:fill-green-900/20";
  return "fill-white dark:fill-gray-800";
}

function getCellStroke(node: ScreenNode): string {
  if (node.isTarget) return "#3b82f6";
  if (node.isOnRoute) return "#22c55e";
  if (!node.isWalkable) return "#9ca3af";
  return "#e5e7eb";
}
