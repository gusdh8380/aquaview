const BASE_URL = "http://localhost:8000/api";

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
