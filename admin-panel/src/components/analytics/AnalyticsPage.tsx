import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ChartBarIcon, UsersIcon, DocumentTextIcon, CurrencyDollarIcon, ArrowTrendingUpIcon, CalendarIcon } from '@heroicons/react/24/outline';
import { analyticsAPI, dashboardAPI } from '../../services/api';
import { RevenueChart } from '../dashboard/RevenueChart';
import { UserGrowthChart } from '../dashboard/UserGrowthChart';
import { clsx } from 'clsx';

interface LicenseDistribution {
  type: string;
  count: number;
  percentage: number;
}

export function AnalyticsPage() {
  const [selectedPeriod, setSelectedPeriod] = useState<'week' | 'month' | 'year'>('month');

  const { data: licenseDistributionData, isLoading: licenseDistributionLoading, error: licenseDistributionError } = useQuery<{
    data: LicenseDistribution[];
  }>({ // Explicitly define the type of data expected
    queryKey: ['license-distribution'],
    queryFn: () => analyticsAPI.getLicenseDistribution(),
  });

  const { data: dashboardStats, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardAPI.getStats(),
    refetchInterval: 300000, // 5 minutes
  });

  if (licenseDistributionError) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Lisans dağılımı yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  const licenseDistribution = licenseDistributionData?.data || [];

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Analitik ve Raporlar</h1>
        <p className="text-gray-600">Sistem performansını, kullanıcı büyümesini ve gelir metriklerini takip edin.</p>
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <RevenueChart />
        <UserGrowthChart />
      </div>

      {/* License Distribution */}
      <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
        <div className="flex items-center space-x-2 mb-6">
          <DocumentTextIcon className="w-6 h-6 text-gray-400" />
          <h3 className="text-xl font-semibold text-gray-900">Lisans Dağılımı</h3>
        </div>
        {licenseDistributionLoading ? (
          <div className="h-48 flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
          </div>
        ) : licenseDistribution.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <DocumentTextIcon className="w-14 h-14 mx-auto mb-3 opacity-50" />
            <p className="text-lg">Lisans dağılım verisi bulunamadı.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {licenseDistribution.map((item, index) => (
              <div key={index} className="bg-gray-50 p-4 rounded-lg border border-gray-100 flex items-center space-x-4">
                <div className="flex-shrink-0 w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
                  <DocumentTextIcon className="w-6 h-6 text-primary-600" />
                </div>
                <div>
                  <p className="text-lg font-semibold text-gray-900">{item.type}</p>
                  <p className="text-sm text-gray-600">{item.count} Lisans ({item.percentage}%)</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Summary Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statsLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 animate-pulse">
              <div className="h-4 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="h-8 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/3"></div>
            </div>
          ))
        ) : (
          <>
            <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 hover:shadow-lg transition-all duration-300">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-2">
                  <UsersIcon className="w-5 h-5 text-primary-600" />
                  <span className="text-sm font-medium text-gray-600">Toplam Kullanıcı</span>
                </div>
                <ArrowTrendingUpIcon className="w-4 h-4 text-green-500" />
              </div>
              <div className="text-3xl font-bold text-gray-900 mb-1">
                {dashboardStats?.data?.totalUsers?.toLocaleString() || '0'}
              </div>
              <div className="text-sm text-gray-500">
                <span className="text-green-600 font-medium">+{dashboardStats?.data?.userGrowth || 0}%</span> bu ay
              </div>
            </div>

            <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 hover:shadow-lg transition-all duration-300">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-2">
                  <DocumentTextIcon className="w-5 h-5 text-blue-600" />
                  <span className="text-sm font-medium text-gray-600">Aktif Lisans</span>
                </div>
                <ArrowTrendingUpIcon className="w-4 h-4 text-blue-500" />
              </div>
              <div className="text-3xl font-bold text-gray-900 mb-1">
                {dashboardStats?.data?.activeLicenses?.toLocaleString() || '0'}
              </div>
              <div className="text-sm text-gray-500">
                Toplam {dashboardStats?.data?.totalLicenses || 0} lisansın {Math.round(((dashboardStats?.data?.activeLicenses || 0) / (dashboardStats?.data?.totalLicenses || 1)) * 100)}%'si
              </div>
            </div>

            <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 hover:shadow-lg transition-all duration-300">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-2">
                  <CurrencyDollarIcon className="w-5 h-5 text-green-600" />
                  <span className="text-sm font-medium text-gray-600">Toplam Gelir</span>
                </div>
                <ArrowTrendingUpIcon className="w-4 h-4 text-green-500" />
              </div>
              <div className="text-3xl font-bold text-gray-900 mb-1">
                ₺{(dashboardStats?.data?.totalRevenue || 0).toLocaleString()}
              </div>
              <div className="text-sm text-gray-500">
                <span className="text-green-600 font-medium">+{dashboardStats?.data?.revenueGrowth || 0}%</span> bu ay
              </div>
            </div>

            <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 hover:shadow-lg transition-all duration-300">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-2">
                  <ChartBarIcon className="w-5 h-5 text-purple-600" />
                  <span className="text-sm font-medium text-gray-600">Ortalama Gelir</span>
                </div>
                <CalendarIcon className="w-4 h-4 text-purple-500" />
              </div>
              <div className="text-3xl font-bold text-gray-900 mb-1">
                ₺{Math.round((dashboardStats?.data?.totalRevenue || 0) / Math.max(dashboardStats?.data?.totalUsers || 1, 1)).toLocaleString()}
              </div>
              <div className="text-sm text-gray-500">
                Kullanıcı başına
              </div>
            </div>
          </>
        )}
      </div>

      {/* Period Selector for Overall Analytics */}
      <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-2">
            <ChartBarIcon className="w-6 h-6 text-gray-400" />
            <h3 className="text-xl font-semibold text-gray-900">Detaylı Analitik</h3>
          </div>
          
          {/* Period Selector */}
          <div className="flex items-center space-x-1 bg-gray-100 rounded-lg p-1">
            {(['week', 'month', 'year'] as const).map((period) => (
              <button
                key={period}
                onClick={() => setSelectedPeriod(period)}
                className={clsx(
                  'px-3 py-1 text-sm font-medium rounded-md transition-colors',
                  selectedPeriod === period
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                )}
              >
                {period === 'week' ? 'Haftalık' : period === 'month' ? 'Aylık' : 'Yıllık'}
              </button>
            ))}
          </div>
        </div>
        
        <p className="text-gray-600 mb-4">
          {selectedPeriod === 'week' ? 'Son 7 gün' : selectedPeriod === 'month' ? 'Son 30 gün' : 'Son 12 ay'} için sistem performans metrikleri.
        </p>
      </div>
    </div>
  );
}
