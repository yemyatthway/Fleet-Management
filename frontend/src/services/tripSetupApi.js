const API_BASE_URL =
  import.meta.env.VITE_TRIP_SETUP_API_BASE_URL || "http://localhost:5215";
import { getAuthHeaders } from "./apiAuth";

const parseResponse = async (response) => {
  if (response.status === 204) return null;
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

const request = async (path, options = {}) => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeaders(),
      ...options.headers,
    },
    ...options,
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

export const createTripSetupApi = (basePath) => ({
  list: (params = {}) => request(`${basePath}${toQueryString(params)}`),
  options: () => request(`${basePath}/options`),
  create: (payload) =>
    request(basePath, { method: "POST", body: JSON.stringify(payload) }),
  update: (id, payload) =>
    request(`${basePath}/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  delete: (id) => request(`${basePath}/${id}`, { method: "DELETE" }),
});

export const tripTypesApi = createTripSetupApi("/api/trip-types");
export const cargoTypesApi = createTripSetupApi("/api/cargo-types");
export const statusesApi = createTripSetupApi("/api/statuses");
export const tripPrioritiesApi = createTripSetupApi("/api/trip-priorities");
export const incidentTypesApi = createTripSetupApi("/api/incident-types");
export const severitiesApi = createTripSetupApi("/api/severities");
export const expenseTypesApi = createTripSetupApi("/api/expense-types");
export const maintenanceTypesApi = createTripSetupApi("/api/maintenance-types");
export const documentTypesApi = createTripSetupApi("/api/document-types");
export const suppliersApi = createTripSetupApi("/api/suppliers");
