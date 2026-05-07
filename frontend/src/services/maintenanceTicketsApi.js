import {
  createApiClient,
  DEFAULT_API_BASE_URL,
  toQueryString,
} from "./httpClient";

const { request } = createApiClient(
  import.meta.env.VITE_MAINTENANCE_TICKETS_API_BASE_URL || DEFAULT_API_BASE_URL,
);

export const getMaintenanceTickets = (params = {}) =>
  request(`/api/maintenance-tickets${toQueryString(params)}`);

export const createMaintenanceTicket = (payload) =>
  request("/api/maintenance-tickets", {
    method: "POST",
    body: JSON.stringify(payload),
  });

export const updateMaintenanceTicket = (ticketId, payload) =>
  request(`/api/maintenance-tickets/${ticketId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });

export const updateMaintenanceTicketStatus = (ticketId, status) =>
  request(`/api/maintenance-tickets/${ticketId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });

export const deleteMaintenanceTicket = (ticketId) =>
  request(`/api/maintenance-tickets/${ticketId}`, {
    method: "DELETE",
  });
