import {
  createApiClient,
  DEFAULT_API_BASE_URL,
  toQueryString,
} from "./httpClient";

const { request } = createApiClient(
  import.meta.env.VITE_DEPARTMENTS_API_BASE_URL || DEFAULT_API_BASE_URL,
);

export const getDepartmentCodeOptions = (params = {}) =>
  request(`/api/departments${toQueryString(params)}`);

export const getDepartmentOptions = () => request("/api/departments/options");

export const createDepartmentCodeOption = (payload) =>
  request("/api/departments", {
    method: "POST",
    body: JSON.stringify(payload),
  });

export const updateDepartmentCodeOption = (id, payload) =>
  request(`/api/departments/${id}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });

export const deleteDepartmentCodeOption = (id) =>
  request(`/api/departments/${id}`, {
    method: "DELETE",
  });
