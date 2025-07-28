import type { User } from './user';
import type { Payment } from './payment';

// Dashboard Stats
export interface DashboardStats {
  totalUsers: number;
  activeUsers: number;
  totalRevenue: number;
  monthlyRevenue: number;
  activeLicenses: number;
  trialUsers: number;
  recentSignups: User[];
  recentPayments: Payment[];
}

// System Status
export interface SystemStatus {
  webApiStatus: 'healthy' | 'degraded' | 'down';
  databaseStatus: 'healthy' | 'degraded' | 'down';
  lastChecked: string;
  uptime: number;
  version: string;
}

// Chart Data Types
export interface ChartDataPoint {
  date: string;
  value: number;
  label?: string;
}

export interface RevenueChartData {
  daily: ChartDataPoint[];
  monthly: ChartDataPoint[];
  yearly: ChartDataPoint[];
}