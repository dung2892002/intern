import type { ActionTree } from 'vuex/types/index.js'
import axios from 'axios'
import type { Employee } from '@/entities/Employee'
import type { State } from './types'
import type { LoginModel } from '@/entities/LoginModel'
import Cookies from 'js-cookie'
import type { Import } from '@/entities/Import'
import type { Customer } from '@/entities/Customer'

const baseUrl = 'https://localhost:7160/api/v1'
interface Response {
  success: boolean
  message?: string
}

export const actions: ActionTree<State, State> = {
  async fetchEmployees(
    { commit },
    { pageSize, pageNumber, keyword, departmentId, positionId, token } = {},
  ) {
    const response = await axios.get(`${baseUrl}/Employees/filter`, {
      params: {
        pageSize: pageSize,
        pageNumber: pageNumber,
        employeeFilter: keyword,
        departmentId: departmentId,
        positionId: positionId,
      },
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    const employees: Employee[] = response.data.Data
    const totalRecords: number = response.data.TotalRecords
    const totalPages: number = response.data.TotalPages
    commit('setEmployees', employees)
    commit('setTotalRecords', totalRecords)
    commit('setTotalPages', totalPages)
  },

  async downloadExcel(_, { datas, token, object } = {}) {
    const response = await axios.post(`${baseUrl}/${object}/export`, datas, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      responseType: 'blob',
    })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `${object}_exports.xlsx`)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  },

  async importExcel({ commit }, { formData, token, object } = {}): Promise<Response> {
    try {
      const response = await axios.post(`${baseUrl}/${object}/import`, formData, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
        params: {
          step: 1,
        },
      })
      const data = response.data
      commit('setDatasImport', data.Datas)
      commit('setTotalValid', data.TotalValid)
      commit('setTotalInValid', data.TotalInvalid)
      localStorage.setItem('ValidDataCacheIdImport', data.ValidDataCacheId)
      localStorage.setItem('InvalidDataCacheIdImport', data.InvalidDataCacheId)
      return { success: true, message: 'Thêm dữ liệu từ file thành công' }
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const serverMessage = error.response?.data || 'Server error occurred'
        return { success: false, message: serverMessage }
      }

      return { success: false, message: 'An unknown error occurred' }
    }
  },

  async addDataImport(_, { cacheId, token, object } = {}): Promise<Response> {
    try {
      await axios.post(
        `${baseUrl}/${object}/import/${cacheId}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          responseType: 'blob',
        },
      )
      return { success: true, message: 'Thêm dữ liệu từ file thành công' }
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const serverMessage = error.response?.data || 'Server error occurred'
        return { success: false, message: serverMessage }
      }

      return { success: false, message: 'An unknown error occurred' }
    }
  },

  async downloadFileImportData(_, { validCacheId, invalidCacheId, token, object } = {}) {
    const response = await axios.post(
      `${baseUrl}/${object}/import/result`,
      {},
      {
        params: {
          validCacheId: validCacheId,
          invalidCacheId: invalidCacheId,
        },
        headers: {
          Authorization: `Bearer ${token}`,
        },
        responseType: 'blob',
      },
    )
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `${object}_import_result.xlsx`)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  },

  async createEmployee(_, employeeDto: Employee): Promise<boolean> {
    try {
      const response = await axios.post(`${baseUrl}/Employees`, employeeDto)
      if (response.status === 201) return true
      return false
    } catch (error) {
      console.error('Lỗi tạo nhân viên:', error)
      return false
    }
  },

  async updateEmployee(_, employeeDto: Employee): Promise<boolean> {
    try {
      const response = await axios.put(
        `${baseUrl}/Employees/${employeeDto.EmployeeId}`,
        employeeDto,
      )
      if (response.status === 200) return true
      return false
    } catch (error) {
      console.error('Lỗi tạo nhân viên:', error)
      return false
    }
  },

  async fetchDepartments({ commit }) {
    try {
      const response = await axios.get(`${baseUrl}/Departments`)
      const departments = response.data
      commit('setDepartments', departments)
    } catch (error) {
      console.log('Failed to fetch departments: ', error)
    }
  },

  async fetchPositions({ commit }) {
    try {
      const response = await axios.get(`${baseUrl}/Positions`)
      const positions = response.data
      commit('setPositions', positions)
    } catch (error) {
      console.log('Failed to fetch Positions: ', error)
    }
  },

  async fetchNewEmployeeCode({ commit }) {
    try {
      const response = await axios.get(`${baseUrl}/Employees/NewEmployeeCode`)
      const newEmployeeCode = response.data
      commit('setNewEmployeeCode', newEmployeeCode)
    } catch (error) {
      console.log('Failed to fetch new employee code: ', error)
    }
  },

  async fetchEmployee({ commit }, id: string) {
    try {
      const response = await axios.get(`${baseUrl}/Employees/${id}`)
      const employee: Employee = response.data
      commit('setEmployee', employee)
    } catch (error) {
      console.log('Failed to fetch employee data: ', error)
    }
  },

  clearEmployees({ commit }) {
    commit('setEmployees', [])
  },

  async login({ commit }, model: LoginModel) {
    try {
      const response = await axios.post(`${baseUrl}/Auths/login`, model)
      if (response.status === 200) {
        localStorage.setItem('accessToken', response.data.Token)
        commit('setAccessToken', response.data.Token)

        localStorage.setItem('username', response.data.Username)
        commit('setUsername', response.data.Username)

        localStorage.setItem('expirationTime', response.data.Expiration)

        Cookies.set('refreshToken', response.data.RefreshToken, {
          expires: 7,
          secure: true,
          sameSite: 'Strict',
        })

        return true
      }
      return false
    } catch (error) {
      console.log('Failed to login: ', error)
      return false
    }
  },

  async logout({ commit }, request) {
    try {
      const response = await axios.post(
        `${baseUrl}/Auths/revoke/${request.username}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${request.token}`,
          },
        },
      )

      if (response.status === 204) {
        commit('setAccessToken', null)
        commit('setUsername', null)
        localStorage.removeItem('accessToken')
        localStorage.removeItem('username')
        localStorage.removeItem('expirationTime')
        Cookies.removeItem('refreshToken')
        return true
      }
    } catch (error) {
      console.log('Failed to login: ', error)
      return false
    }
  },

  async refreshToken({ commit }) {
    const expirationTime = localStorage.getItem('expirationTime')
    const currentTime = new Date().getTime()
    const expiration = new Date(expirationTime || 0).getTime()
    if (expirationTime && expiration - currentTime < 61000) {
      try {
        const request = {
          AccessToken: localStorage.getItem('accessToken') || null,
          RefreshToken: Cookies.get('refreshToken'),
        }

        const response = await axios.post(`${baseUrl}/Auths/refresh-token`, request)
        if (response.status === 200) {
          localStorage.setItem('accessToken', response.data.AccessToken)
          commit('setAccessToken', response.data.Token)

          localStorage.setItem('expirationTime', response.data.Expiration)

          Cookies.set('refreshToken', response.data.RefreshToken, {
            expires: 7,
            secure: true,
            sameSite: 'Strict',
          })

          return true
        } else {
          localStorage.removeItem('accessToken')
          localStorage.removeItem('expirationTime')
          localStorage.removeItem('username')
        }
      } catch (error) {
        localStorage.removeItem('accessToken')
        localStorage.removeItem('expirationTime')
        localStorage.removeItem('username')
        console.log('Failed to login: ', error)
        return false
      }
    }
  },

  async fecthImportSetting({ commit }) {
    try {
      const response = await axios.get(`${baseUrl}/Imports`)
      commit('setImportSettings', response.data)
    } catch (error) {
      console.log('Failed to login: ', error)
      return false
    }
  },

  async createImport(_, importValue): Promise<boolean> {
    try {
      const response = await axios.post(`${baseUrl}/Imports`, importValue)
      if (response.status === 201) return true
      return false
    } catch (error) {
      console.log('Failed to create: ', error)
      return false
    }
  },

  async updateImport(_, importValue: Import): Promise<boolean> {
    try {
      const response = await axios.put(`${baseUrl}/Imports/${importValue.Id}`, importValue)
      if (response.status === 200) return true
      return false
    } catch (error) {
      console.log('Failed to update: ', error)
      return false
    }
  },
  async deleteImport(_, importValue: Import): Promise<boolean> {
    try {
      const response = await axios.delete(`${baseUrl}/Imports`, {
        params: {
          id: importValue.Id,
        },
      })
      if (response.status === 200) return true
      return false
    } catch (error) {
      console.log('Failed to update: ', error)
      return false
    }
  },

  async fetchCustomers({ commit }, { pageSize, pageNumber, keyword, token } = {}) {
    const response = await axios.get(`${baseUrl}/Customers/filter`, {
      params: {
        pageSize: pageSize,
        pageNumber: pageNumber,
        keyword: keyword,
      },
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    const customers: Customer[] = response.data.Data
    const totalRecords: number = response.data.TotalRecords
    const totalPages: number = response.data.TotalPages
    commit('setCustomers', customers)
    commit('setTotalRecords', totalRecords)
    commit('setTotalPages', totalPages)
  },
}
