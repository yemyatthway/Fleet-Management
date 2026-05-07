import {
  createApiClient,
  DEFAULT_API_BASE_URL,
  resolveAssetUrl,
  toQueryString,
} from "./httpClient";

const { request, baseUrl } = createApiClient(
  import.meta.env.VITE_ROLES_API_BASE_URL || DEFAULT_API_BASE_URL,
);

export const getRoles = (params = {}) =>
  request(`/api/roles${toQueryString(params)}`);

export const getRoleOptions = () => request("/api/roles/options");

export const getRoleMembers = async (roleId) => {
  const result = await request(`/api/roles/${roleId}/members`);
  return Array.isArray(result)
    ? result.map((member) => ({
        ...member,
        avatar: resolveAssetUrl(member?.avatar, baseUrl),
      }))
    : [];
};

export const createRole = (role) =>
  request("/api/roles", {
    method: "POST",
    body: JSON.stringify(role),
  });

export const updateRole = (roleId, role) =>
  request(`/api/roles/${roleId}`, {
    method: "PUT",
    body: JSON.stringify(role),
  });

export const deleteRole = (roleId) =>
  request(`/api/roles/${roleId}`, {
    method: "DELETE",
  });
