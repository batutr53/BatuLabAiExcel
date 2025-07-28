import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  UsersIcon, 
  CreditCardIcon, 
  DocumentTextIcon, 
  TrendingUpIcon,
  ClockIcon,
  CheckCircleIcon
} from '@heroicons/react/24/outline';
import { dashboardAPI } from '../../services/api';
import { StatsCard } from './StatsCard';
import { RecentActivity } from './RecentActivity';
import { RevenueChart } from './RevenueChart';
import { UserGrowthChart } from './UserGrowthChart';
import { SystemStatus } from './SystemStatus';
import { QuickActions } from './QuickActions';

export function DashboardPage() {
  const { data: statsData, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardAPI.getStats(),
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  const { data: systemData, isLoading: systemLoading } = useQuery({
    queryKey: ['system-status'],
    queryFn: () => dashboardAPI.getSystemStatus(),
    refetchInterval: 10000, // Refetch every 10 seconds
  });

  const stats = statsData?.data;
  const systemStatus = systemData?.data;

  if (statsLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-lg p-6 shadow-sm animate-pulse">
              <div className="h-4 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="h-8 bg-gray-200 rounded w-3/4"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  const statCards = [
    {
      title: 'Toplam Kullanıcı',
      value: stats?.totalUsers || 0,
      change: '+12%',
      trend: 'up' as const,
      icon: UsersIcon,
      color: 'blue',
    },
    {
      title: 'Aktif Lisans',
      value: stats?.activeLicenses || 0,
      change: '+8%',
      trend: 'up' as const,
      icon: DocumentTextIcon,
      color: 'green',
    },
    {
      title: 'Aylık Gelir',
      value: `₺${(stats?.monthlyRevenue || 0).toLocaleString()}`,
      change: '+23%',
      trend: 'up' as const,
      icon: CreditCardIcon,
      color: 'yellow',
    },
    {
      title: 'Aktif Kullanıcı',
      value: stats?.activeUsers || 0,
      change: '+15%',
      trend: 'up' as const,
      icon: TrendingUpIcon,
      color: 'purple',
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600 mt-1">
            Office AI yönetim paneline hoş geldiniz
          </p>
        </div>
        <div className="flex items-center space-x-2 text-sm text-gray-500">
          <ClockIcon className="h-4 w-4" />
          <span>Son güncelleme: {new Date().toLocaleTimeString('tr-TR')}</span>
        </div>
      </div>

      {systemStatus && (
        <SystemStatus status={systemStatus} loading={systemLoading} />
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((stat, index) => (
          <StatsCard key={index} {...stat} />
        ))}
      </div>

      <QuickActions />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RevenueChart />
        <UserGrowthChart />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <RecentActivity 
            recentSignups={stats?.recentSignups || []}
            recentPayments={stats?.recentPayments || []}
          />
        </div>
        
        <div className="space-y-6">
          <div className="bg-white rounded-lg p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Deneme Kullanıcıları</h3>
              <div className="w-3 h-3 bg-orange-400 rounded-full"></div>
            </div>
            <div className="text-3xl font-bold text-orange-600 mb-2">
              {stats?.trialUsers || 0}
            </div>
            <p className="text-sm text-gray-600">Aktif deneme kullanıcısı</p>
          </div>

          <div className="bg-white rounded-lg p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Gelir Özeti</h3>
              <CheckCircleIcon className="w-5 h-5 text-green-500" />
            </div>
            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-gray-600">Bu Ay</span>
                <span className="text-sm font-semibold">
                  ₺{(stats?.monthlyRevenue || 0).toLocaleString()}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-gray-600">Toplam</span>
                <span className="text-sm font-semibold">
                  ₺{(stats?.totalRevenue || 0).toLocaleString()}
                </span>
              </div>
              <div className="pt-2 border-t border-gray-100">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Ort. Kullanıcı Değeri</span>
                  <span className="text-sm font-semibold text-green-600">
                    ₺{stats?.totalUsers ? Math.round((stats.totalRevenue || 0) / stats.totalUsers) : 0}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}