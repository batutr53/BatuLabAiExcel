import type { User } from './user';
import type { Payment } from './payment';

// Dashboard Stats (Backend format)
export interface DashboardStats {
  totalUsers: number;
  activeUsers: number;
  totalRevenue: number;
  totalLicenses: number;
  activeLicenses: number;
  revenueGrowth: number;
  userGrowth: number;
}

// System Status (Backend format)
export interface SystemStatus {
  database: {
    status: string;
    responseTime: string;
  };
  api: {
    status: string;
    responseTime: string;
  };
  storage: {
    status: string;
    usage: string;
  };
  memory: {
    status: string;
    usage: string;
  };
  lastUpdated: string;
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