const BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:8000/api";

export async function fetchSensors() {
  const res = await fetch(`${BASE_URL}/sensors`);
  if (!res.ok) throw new Error("Failed to fetch sensors");
  const data = await res.json();
  return data.sensors;
}

export async function fetchAlerts() {
  const res = await fetch(`${BASE_URL}/alerts`);
  if (!res.ok) throw new Error("Failed to fetch alerts");
  const data = await res.json();
  return data.alerts;
}

export async function fetchHistory(sensor, limit = 20) {
  const res = await fetch(`${BASE_URL}/history?sensor=${sensor}&limit=${limit}`);
  if (!res.ok) throw new Error("Failed to fetch history");
  return res.json();
}

// ── Pipeline API ─────────────────────────────────────────────────────

/** GET /api/pipeline — default HRT (ratio=1.0) result */
export async function fetchPipeline() {
  const res = await fetch(`${BASE_URL}/pipeline`);
  if (!res.ok) throw new Error("Failed to fetch pipeline");
  return res.json();
}

/**
 * POST /api/pipeline/params — recalculate with custom HRT ratios
 * @param {Array<{stage: string, hrt_ratio: number}>} params
 */
export async function updatePipelineParams(params) {
  const res = await fetch(`${BASE_URL}/pipeline/params`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ params }),
  });
  if (!res.ok) throw new Error("Failed to update pipeline params");
  return res.json();
}
