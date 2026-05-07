import { getAuthHeaders } from "./apiAuth";

const API_BASE_URL =
  import.meta.env.VITE_AUDIT_API_BASE_URL || "http://localhost:5215";

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

const request = async (path) => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: getAuthHeaders(),
  });
  return parseResponse(response);
};

const toQueryString = (params = {}) => {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "")
      query.set(key, value);
  });
  const value = query.toString();
  return value ? `?${value}` : "";
};

export const getAuditLogs = (params = {}) =>
  request(`/api/audit-logs${toQueryString(params)}`);
export const getStatusHistory = (params = {}) =>
  request(`/api/status-history${toQueryString(params)}`);
