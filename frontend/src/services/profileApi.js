import { createApiClient, DEFAULT_API_BASE_URL, resolveAssetUrl } from './httpClient'

const { request, baseUrl } = createApiClient(import.meta.env.VITE_PROFILE_API_BASE_URL || DEFAULT_API_BASE_URL)

const normalizeProfile = (profile) => ({
  ...profile,
  avatar: resolveAssetUrl(profile?.avatar, baseUrl),
  nrcFront: resolveAssetUrl(profile?.nrcFront, baseUrl),
  nrcBack: resolveAssetUrl(profile?.nrcBack, baseUrl)
})

export const getProfile = async () => normalizeProfile(await request('/api/profile'))

export const changePassword = (payload) =>
  request('/api/profile/change-password', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
