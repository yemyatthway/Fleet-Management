const API_BASE_URL =
  import.meta.env.VITE_DASHBOARD_API_BASE_URL || "http://localhost:5215";
import { getAuthHeaders } from "./apiAuth";

const parseResponse = async (response) => {
  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json")
    ? await response.json()
    : null;
  if (!response.ok)
    throw new Error(
      body?.message || `Request failed with status ${response.status}`,
    );
  return body;
};

export const getDashboardSummary = async () => {
  const response = await fetch(`${API_BASE_URL}/api/dashboard/summary`, {
    headers: getAuthHeaders(),
  });
  return parseResponse(response);
};
