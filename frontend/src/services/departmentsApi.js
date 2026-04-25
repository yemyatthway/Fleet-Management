import { fakeBackend } from '../data/fakeBackend'

const DEPARTMENT_TYPE = 'Department'

export const getDepartmentCodeOptions = (params = {}) => fakeBackend.getDepartmentCodeOptions(params)

export const getDepartmentOptions = () => fakeBackend.getDepartmentOptions()

export const createDepartmentCodeOption = (payload) =>
  fakeBackend.createUserCodeOption({ ...payload, type: DEPARTMENT_TYPE })

export const updateDepartmentCodeOption = (id, payload) =>
  fakeBackend.updateUserCodeOption(id, { ...payload, type: DEPARTMENT_TYPE })

export const deleteDepartmentCodeOption = (id) =>
  fakeBackend.deleteUserCodeOption(id, DEPARTMENT_TYPE)
