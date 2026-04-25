import { fakeBackend } from '../data/fakeBackend'

const LOCATION_TYPE = 'Location'

export const getLocationCodeOptions = (params = {}) => fakeBackend.getLocationCodeOptions(params)

export const getLocationOptions = () => fakeBackend.getLocationOptions()

export const createLocationCodeOption = (payload) =>
  fakeBackend.createUserCodeOption({ ...payload, type: LOCATION_TYPE })

export const updateLocationCodeOption = (id, payload) =>
  fakeBackend.updateUserCodeOption(id, { ...payload, type: LOCATION_TYPE })

export const deleteLocationCodeOption = (id) =>
  fakeBackend.deleteUserCodeOption(id, LOCATION_TYPE)
