import axios from 'axios';
import type { AxiosInstance, AxiosResponse } from 'axios';
import type { 
  User, 
  License, 
  Payment, 
  DashboardStats, 
  SystemStatus, 
  ApiResponse, 
  PaginatedResponse,
  FilterState,
  LoginCredentials,
  AdminUser
} from '../types';

// API Client Configuration
class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: (import.meta.env.VITE_API_BASE_URL as string) || 'https://localhost:59640/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('admin_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error: unknown) => Promise.reject(new Error(error instanceof Error ? error.message : 'Request failed'))
    );

    // Response interceptor for global error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error: unknown) => {
        if (error && typeof error === 'object' && 'response' in error) {
          const axiosError = error as { response: { status: number } };
          if (axiosError.response?.status === 401) {
            localStorage.removeItem('admin_token');
            window.location.href = '/login';
          }
        }
        return Promise.reject(new Error(error instanceof Error ? error.message : 'Request failed'));
      }
    );
  }

  // Generic API methods
  private async get<T>(url: string, params?: Record<string, unknown> | FilterState | LoginCredentials): Promise<T> {
    const response: AxiosResponse<T> = await this.client.get(url, { params });
    return response.data;
  }

  private async post<T>(url: string, data?: Record<string, unknown> | LoginCredentials): Promise<T> {
    const response: AxiosResponse<T> = await this.client.post(url, data);
    return response.data;
  }

  private async put<T>(url: string, data?: Record<string, unknown>): Promise<T> {
    const response: AxiosResponse<T> = await this.client.put(url, data);
    return response.data;
  }

  private async delete<T>(url: string): Promise<T> {
    const response: AxiosResponse<T> = await this.client.delete(url);
    return response.data;
  }

  // Authentication API
  async login(credentials: LoginCredentials): Promise<{ success: boolean; message: string; token?: string; user?: AdminUser; errors: string[] }> {
    return this.post('/auth/login', { email: credentials.email, password: credentials.password });
  }

  async logout(): Promise<ApiResponse<void>> {
    return this.post('/auth/logout');
  }

  async getProfile(): Promise<ApiResponse<AdminUser>> {
    return this.get('/auth/profile');
  }

  // Dashboard API
  async getDashboardStats(): Promise<ApiResponse<DashboardStats>> {
    return this.get('/admin/dashboard/stats');
  }

  async getSystemStatus(): Promise<ApiResponse<SystemStatus>> {
    return this.get('/admin/system/status');
  }

  // User Management API
  async getUsers(filters: FilterState): Promise<ApiResponse<PaginatedResponse<User>>> {
    return this.get('/admin/users', filters);
  }

  async getUser(id: string): Promise<ApiResponse<User>> {
    return this.get(`/admin/users/${id}`);
  }

  async updateUser(id: string, data: Partial<User>): Promise<ApiResponse<User>> {
    return this.put(`/admin/users/${id}`, data);
  }

  async deleteUser(id: string): Promise<ApiResponse<void>> {
    return this.delete(`/admin/users/${id}`);
  }

  async suspendUser(id: string): Promise<ApiResponse<void>> {
    return this.post(`/admin/users/${id}/suspend`);
  }

  async unsuspendUser(id: string): Promise<ApiResponse<void>> {
    return this.post(`/admin/users/${id}/unsuspend`);
  }

  // License Management API
  async getLicenses(filters: FilterState): Promise<ApiResponse<PaginatedResponse<License>>> {
    return this.get('/admin/licenses', filters);
  }

  async getLicense(id: string): Promise<ApiResponse<License>> {
    return this.get(`/admin/licenses/${id}`);
  }

  async createLicense(data: Partial<License>): Promise<ApiResponse<License>> {
    return this.post('/admin/licenses', data);
  }

  async updateLicense(id: string, data: Partial<License>): Promise<ApiResponse<License>> {
    return this.put(`/admin/licenses/${id}`, data);
  }

  async revokeLicense(id: string): Promise<ApiResponse<void>> {
    return this.post(`/admin/licenses/${id}/revoke`);
  }

  async extendLicense(id: string, days: number): Promise<ApiResponse<object>> {
    return this.post(`/admin/licenses/${id}/extend`, { Days: days });
  }

  async deleteLicense(id: string): Promise<ApiResponse<object>> {
    return this.delete(`/admin/licenses/${id}`);
  }

  // Payment Management API
  async getPayments(filters: FilterState): Promise<ApiResponse<PaginatedResponse<Payment>>> {
    return this.get('/admin/payments', filters);
  }

  async getPayment(id: string): Promise<ApiResponse<Payment>> {
    return this.get(`/admin/payments/${id}`);
  }

  async refundPayment(id: string, reason?: string): Promise<ApiResponse<void>> {
    return this.post(`/admin/payments/${id}/refund`, { reason });
  }

  // Analytics API
  async getRevenueAnalytics(period: 'week' | 'month' | 'year'): Promise<ApiResponse<{ data: Array<{ date: string; value: number }> }>> {
    return this.get(`/admin/analytics/revenue?period=${period}`);
  }

  async getUserGrowthAnalytics(period: 'week' | 'month' | 'year'): Promise<ApiResponse<{ data: Array<{ date: string; value: number }> }>> {
    return this.get(`/admin/analytics/users?period=${period}`);
  }

  async getLicenseDistribution(): Promise<ApiResponse<Record<string, unknown>>> {
    return this.get('/admin/analytics/license-distribution');
  }

  // System Management API
  async getSystemLogs(filters: FilterState): Promise<ApiResponse<PaginatedResponse<Record<string, unknown>>>> {
    return this.get('/admin/system/logs', filters);
  }

  async clearSystemLogs(): Promise<ApiResponse<void>> {
    return this.post('/admin/system/logs/clear');
  }

  async getHealthCheck(): Promise<ApiResponse<SystemStatus>> {
    return this.get('/admin/system/health');
  }

  // Notification API
  async sendNotification(userIds: string[], message: string, type: string): Promise<ApiResponse<void>> {
    return this.post('/admin/notifications/send', { userIds, message, type });
  }

  async broadcastNotification(message: string, type: string): Promise<ApiResponse<void>> {
    return this.post('/admin/notifications/broadcast', { message, type });
  }
}

// Export singleton instance
export const apiClient = new ApiClient();

// Export individual API modules for better organization
export const authAPI = {
  login: (credentials: LoginCredentials) => apiClient.login(credentials),
  logout: () => apiClient.logout(),
  getProfile: () => apiClient.getProfile(),
};

export const dashboardAPI = {
  getStats: () => apiClient.getDashboardStats(),
  getSystemStatus: () => apiClient.getSystemStatus(),
};

export const userAPI = {
  getUsers: (filters: FilterState) => apiClient.getUsers(filters),
  getUser: (id: string) => apiClient.getUser(id),
  updateUser: (id: string, data: Partial<User>) => apiClient.updateUser(id, data),
  deleteUser: (id: string) => apiClient.deleteUser(id),
  suspendUser: (id: string) => apiClient.suspendUser(id),
  unsuspendUser: (id: string) => apiClient.unsuspendUser(id),
};

export const licenseAPI = {
  getLicenses: (filters: FilterState) => apiClient.getLicenses(filters),
  getLicense: (id: string) => apiClient.getLicense(id),
  createLicense: (data: Partial<License>) => apiClient.createLicense(data),
  updateLicense: (id: string, data: Partial<License>) => apiClient.updateLicense(id, data),
  revokeLicense: (id: string) => apiClient.revokeLicense(id),
  extendLicense: (id: string, days: number) => apiClient.extendLicense(id, days),
  deleteLicense: (id: string) => apiClient.deleteLicense(id),
};

export const paymentAPI = {
  getPayments: (filters: FilterState) => apiClient.getPayments(filters),
  getPayment: (id: string) => apiClient.getPayment(id),
  refundPayment: (id: string, reason?: string) => apiClient.refundPayment(id, reason),
};

export const analyticsAPI = {
  getRevenueAnalytics: (period: 'week' | 'month' | 'year') => apiClient.getRevenueAnalytics(period),
  getUserGrowthAnalytics: (period: 'week' | 'month' | 'year') => apiClient.getUserGrowthAnalytics(period),
  getLicenseDistribution: () => apiClient.getLicenseDistribution(),
};