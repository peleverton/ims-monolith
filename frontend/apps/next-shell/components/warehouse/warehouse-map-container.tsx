"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "@/lib/api-client";
import {
  adaptLayoutToScreen,
  type RouteResult,
  type WarehouseLayoutDto,
  type WarehouseMapData,
} from "@/lib/warehouse-map-adapter";
import { WarehouseGrid } from "./warehouse-grid";

/**
 * Epic 5: Container Component — handles data fetching, state, and animation.
 * Separated from the presentational WarehouseGrid (Container/Presentational pattern).
 */

export function WarehouseMapContainer() {
  const [layout, setLayout] = useState<WarehouseLayoutDto | null>(null);
  const [route, setRoute] = useState<RouteResult | null>(null);
  const [mapData, setMapData] = useState<WarehouseMapData | null>(null);
  const [selectedTargets, setSelectedTargets] = useState<string[]>([]);
  const [animatedEdgeIndex, setAnimatedEdgeIndex] = useState(-1);
  const [loading, setLoading] = useState(true);
  const [calculating, setCalculating] = useState(false);
  const [strategy, setStrategy] = useState<string>("auto");

  // Fetch warehouse layout on mount
  useEffect(() => {
    async function fetchLayout() {
      try {
        const data = await apiFetch<WarehouseLayoutDto>(
          "/api/warehouse-routing/layout"
        );
        setLayout(data);
        setMapData(adaptLayoutToScreen(data));
      } catch {
        // Silent fail
      } finally {
        setLoading(false);
      }
    }
    fetchLayout();
  }, []);

  // Toggle target selection
  const toggleTarget = useCallback(
    (locationId: string) => {
      setSelectedTargets((prev) =>
        prev.includes(locationId)
          ? prev.filter((id) => id !== locationId)
          : [...prev, locationId]
      );
      // Reset route when selection changes
      setRoute(null);
      setAnimatedEdgeIndex(-1);
    },
    []
  );

  // Animate route edges one by one
  const animateRoute = useCallback((totalEdges: number) => {
    setAnimatedEdgeIndex(-1);

    let current = -1;
    const interval = setInterval(() => {
      current++;
      setAnimatedEdgeIndex(current);
      if (current >= totalEdges) {
        clearInterval(interval);
      }
    }, 400);
  }, []);

  // Calculate route
  const calculateRoute = useCallback(async () => {
    if (!layout || selectedTargets.length === 0) return;

    setCalculating(true);
    try {
      const result = await apiFetch<RouteResult>(
        "/api/warehouse-routing/calculate",
        {
          method: "POST",
          body: JSON.stringify({
            targetLocationIds: selectedTargets,
            strategy: strategy === "auto" ? null : strategy,
          }),
        }
      );
      setRoute(result);
      setMapData(adaptLayoutToScreen(layout, result, selectedTargets));
      // Start animation
      animateRoute(result.steps.length - 1);
    } catch {
      // Silent
    } finally {
      setCalculating(false);
    }
  }, [layout, selectedTargets, strategy, animateRoute]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500" />
      </div>
    );
  }

  if (!mapData || !layout) {
    return (
      <div className="text-center py-12 text-gray-500 dark:text-gray-400">
        Nenhum layout de armazém disponível. Cadastre corredores e prateleiras primeiro.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Mapa do Armazém
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Selecione prateleiras para calcular a rota de coleta ideal
          </p>
        </div>
        <div className="flex items-center gap-3">
          {/* Strategy selector */}
          <select
            value={strategy}
            onChange={(e) => setStrategy(e.target.value)}
            className="text-sm border border-gray-300 dark:border-gray-600 rounded-md px-3 py-1.5 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200"
          >
            <option value="auto">Auto (por tamanho)</option>
            <option value="Dijkstra">Dijkstra (preciso)</option>
            <option value="AStar">A* (rápido)</option>
          </select>

          <button
            onClick={calculateRoute}
            disabled={selectedTargets.length === 0 || calculating}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {calculating ? "Calculando..." : `Calcular Rota (${selectedTargets.length})`}
          </button>
        </div>
      </div>

      {/* Map + Instructions side by side */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* SVG Map */}
        <div className="lg:col-span-2">
          <div
            className="relative cursor-pointer"
            onClick={(e) => {
              // Find clicked node
              const svg = e.currentTarget.querySelector("svg");
              if (!svg) return;
              const rect = svg.getBoundingClientRect();
              const scaleX = mapData.width / rect.width;
              const scaleY = mapData.height / rect.height;
              const x = (e.clientX - rect.left) * scaleX;
              const y = (e.clientY - rect.top) * scaleY;

              // Find nearest node within click radius
              const clickRadius = 32;
              const clicked = mapData.nodes.find(
                (n) =>
                  n.isWalkable &&
                  Math.abs(n.x - x) < clickRadius &&
                  Math.abs(n.y - y) < clickRadius
              );
              if (clicked) toggleTarget(clicked.id);
            }}
          >
            <WarehouseGrid
              nodes={mapData.nodes}
              edges={mapData.edges}
              width={mapData.width}
              height={mapData.height}
              animatedEdgeIndex={animatedEdgeIndex}
            />
          </div>

          {/* Legend */}
          <div className="flex items-center gap-4 mt-3 text-xs text-gray-500 dark:text-gray-400">
            <span className="flex items-center gap-1">
              <span className="w-3 h-3 rounded bg-blue-100 border-2 border-blue-500" />
              Alvo selecionado
            </span>
            <span className="flex items-center gap-1">
              <span className="w-3 h-3 rounded bg-green-50 border border-green-500" />
              Na rota
            </span>
            <span className="flex items-center gap-1">
              <span className="w-3 h-3 rounded bg-gray-300" />
              Obstáculo
            </span>
          </div>
        </div>

        {/* Instructions panel */}
        <div>
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
            Instruções de Coleta
          </h2>

          {route ? (
            <div className="space-y-2">
              {/* Stats */}
              <div className="grid grid-cols-2 gap-2 mb-4">
                <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 p-3 text-center">
                  <p className="text-lg font-bold text-blue-700 dark:text-blue-300">
                    {route.totalDistance}
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Distância</p>
                </div>
                <div className="rounded-lg bg-green-50 dark:bg-green-900/20 p-3 text-center">
                  <p className="text-lg font-bold text-green-700 dark:text-green-300">
                    {route.strategy}
                  </p>
                  <p className="text-xs text-green-600 dark:text-green-400">Algoritmo</p>
                </div>
              </div>

              {/* Step list */}
              <div className="max-h-80 overflow-y-auto space-y-1">
                {route.steps.map((step, i) => (
                  <div
                    key={step.order}
                    className={`flex items-start gap-2 rounded-lg p-2 text-sm transition-colors ${
                      i <= animatedEdgeIndex
                        ? "bg-blue-50 dark:bg-blue-900/20"
                        : "bg-white dark:bg-gray-800"
                    }`}
                  >
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-blue-500 text-white text-xs flex items-center justify-center font-bold">
                      {step.order}
                    </span>
                    <div>
                      <p className="font-medium text-gray-900 dark:text-gray-100">
                        {step.locationCode}
                      </p>
                      <p className="text-gray-500 dark:text-gray-400 text-xs">
                        {step.instruction}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <p className="text-sm text-gray-400 dark:text-gray-500">
              {selectedTargets.length > 0
                ? `${selectedTargets.length} local(is) selecionado(s). Clique "Calcular Rota".`
                : "Clique nas células do mapa para selecionar pontos de coleta."}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
