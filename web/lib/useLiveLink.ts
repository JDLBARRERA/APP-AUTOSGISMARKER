"use client";

import { createClient, type SupabaseClient } from "@supabase/supabase-js";
import { useEffect, useRef, useState } from "react";
import type { LiveState } from "@/lib/sessionBus";

const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
const key = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

function cloudClient(): SupabaseClient | null {
  if (!url || !key) return null;
  return createClient(url, key);
}

export function useLiveLink(onState?: (state: LiveState) => void) {
  const [connected, setConnected] = useState(false);
  const onStateRef = useRef(onState);
  onStateRef.current = onState;

  useEffect(() => {
    const supabase = cloudClient();
    if (supabase) {
      let active = true;
      supabase
        .from("race_session")
        .select("format,module,race,shot,notice")
        .eq("id", 1)
        .maybeSingle()
        .then(({ data }) => {
          if (active && data) onStateRef.current?.(data as LiveState);
        });

      const channel = supabase
        .channel("race-session")
        .on(
          "postgres_changes",
          { event: "*", schema: "public", table: "race_session" },
          (payload) => {
            const next = payload.new as LiveState;
            if (next && typeof next.shot === "number") onStateRef.current?.(next);
          },
        )
        .subscribe((status) => {
          setConnected(status === "SUBSCRIBED");
        });

      return () => {
        active = false;
        supabase.removeChannel(channel);
      };
    }

    const source = new EventSource("/api/session");
    source.onopen = () => setConnected(true);
    source.onerror = () => setConnected(false);
    source.onmessage = (event) => {
      onStateRef.current?.(JSON.parse(event.data) as LiveState);
    };
    return () => source.close();
  }, []);

  return connected;
}

export async function publishSession(state: LiveState) {
  const supabase = cloudClient();
  if (supabase) {
    const { error } = await supabase.from("race_session").upsert({ id: 1, ...state });
    if (!error) return;
  }

  await fetch("/api/session", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(state),
  });
}
