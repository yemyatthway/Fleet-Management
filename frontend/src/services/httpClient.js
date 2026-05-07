import { getAuthHeaders } from "./apiAuth";

export const DEFAULT_API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5215";

export const toQueryString = (params = {}) => {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "")
      query.set(key, value);
  });
  const value = query.toString();
  return value ? `?${value}` : "";
};

export const resolveAssetUrl = (value, baseUrl = DEFAULT_API_BASE_URL) => {
  if (!value) return "";
  if (/^https?:\/\//i.test(value) || value.startsWith("data:")) return value;
  if (/^file:\/\/\/uploads\//i.test(value)) {
    const relativePath = value.replace(/^file:\/\//i, "");
    return `${baseUrl}${relativePath.startsWith("/") ? relativePath : `/${relativePath}`}`;
  }
  if (/^uploads\//i.test(value)) return `${baseUrl}/${value}`;
  if (value.startsWith("/")) return `${baseUrl}${value}`;
  return value;
};

export const parseResponse = async (response) => {
  if (response.status === 204) return null;

  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json")
    ? await response.json()
    : null;

  if (!response.ok) {
    throw new Error(
      body?.message || `Request failed with status ${response.status}`,
    );
  }

  return body;
};

export const createApiClient = (baseUrl = DEFAULT_API_BASE_URL) => {
  const request = async (path, options = {}) => {
    const isFormData = options.body instanceof FormData;
    const response = await fetch(`${baseUrl}${path}`, {
      headers: isFormData
        ? { ...getAuthHeaders(), ...options.headers }
        : {
            "Content-Type": "application/json",
            ...getAuthHeaders(),
            ...options.headers,
          },
      ...options,
    });

    return parseResponse(response);
  };

  return { request, baseUrl };
};
