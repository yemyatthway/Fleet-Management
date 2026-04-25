export function buildReferenceRequest(type, payload) {
  return {
    type,
    name: payload.name,
    description: payload.description || null,
    status: payload.status || 'Active'
  }
}
