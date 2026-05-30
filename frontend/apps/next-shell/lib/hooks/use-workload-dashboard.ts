"use client";

import { useCallback, useEffect, useReducer, useRef } from "react";
import { getSignalRConnection, startConnection } from "@/lib/signalr-client";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface AutoAssignmentEvent {
  id: string;
  issueId: string;
  issueTitle: string;
  priority: string;
  assigneeId: string;
  reason: string;
  occurredOn: string;
}

export interface WorkloadEntry {
  userId: string;
  username: string;
  activeIssues: number;
  overdueIssues: number;
  avgResolutionHours: number;
  score: number;
  skills: string[];
}

export interface WorkloadDashboardState {
  recentAssignments: AutoAssignmentEvent[];
  isConnected: boolean;
  totalAutoAssigned: number;
}

// ─── Reducer ──────────────────────────────────────────────────────────────────

type Action =
  | { type: "NEW_ASSIGNMENT"; payload: AutoAssignmentEvent }
  | { type: "CONNECTION_CHANGED"; connected: boolean }
  | { type: "RESET" };

const MAX_RECENT_EVENTS = 20;

function reducer(state: WorkloadDashboardState, action: Action): WorkloadDashboardState {
  switch (action.type) {
    case "NEW_ASSIGNMENT":
      return {
        ...state,
        recentAssignments: [action.payload, ...state.recentAssignments].slice(0, MAX_RECENT_EVENTS),
        totalAutoAssigned: state.totalAutoAssigned + 1,
      };
    case "CONNECTION_CHANGED":
      return { ...state, isConnected: action.connected };
    case "RESET":
      return initialState;
    default:
      return state;
  }
}

const initialState: WorkloadDashboardState = {
  recentAssignments: [],
  isConnected: false,
  totalAutoAssigned: 0,
};

// ─── Hook ─────────────────────────────────────────────────────────────────────

export function useWorkloadDashboard() {
  const [state, dispatch] = useReducer(reducer, initialState);
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    const conn = getSignalRConnection();

    conn.on("IssueAutoAssigned", (payload: {
      issueId: string;
      issueTitle?: string;
      priority?: string;
      assigneeId: string;
      reason: string;
      occurredOn: string;
    }) => {
      if (!mountedRef.current) return;

      const event: AutoAssignmentEvent = {
        id: `${payload.issueId}-${Date.now()}`,
        issueId: payload.issueId,
        issueTitle: payload.issueTitle ?? "Issue",
        priority: payload.priority ?? "Medium",
        assigneeId: payload.assigneeId,
        reason: payload.reason,
        occurredOn: payload.occurredOn,
      };

      dispatch({ type: "NEW_ASSIGNMENT", payload: event });
    });

    conn.onreconnected(() => {
      if (mountedRef.current) dispatch({ type: "CONNECTION_CHANGED", connected: true });
    });

    conn.onclose(() => {
      if (mountedRef.current) dispatch({ type: "CONNECTION_CHANGED", connected: false });
    });

    startConnection().then(() => {
      if (mountedRef.current) dispatch({ type: "CONNECTION_CHANGED", connected: true });
    });

    return () => {
      mountedRef.current = false;
      conn.off("IssueAutoAssigned");
    };
  }, []);

  const reset = useCallback(() => dispatch({ type: "RESET" }), []);

  return { ...state, reset };
}
