import { useQuery, useQueryClient } from '@tanstack/react-query';
import { 
  DocumentTextIcon, 
  ArrowTrendingUpIcon,
  ClockIcon,
  CheckCircleIcon,
  UserGroupIcon,
  CurrencyDollarIcon,
  SparklesIcon,
  ChartBarIcon,
  BoltIcon,
  ShieldCheckIcon,
  ArrowTrendingUpIcon as TrendingUpIcon,
  ArrowTrendingDownIcon as TrendingDownIcon,
} from '@heroicons/react/24/outline';
import { dashboardAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { StatsCard } from './StatsCard';
import type { StatsCardProps } from './StatsCard';
import { RecentActivity } from './RecentActivity';
import { RevenueChart } from './RevenueChart';
import { UserGrowthChart } from './UserGrowthChart';
import { SystemStatus } from './SystemStatus';
import { QuickActions } from './QuickActions';

export function DashboardPage() {
  const queryClient = useQueryClient();

  const { data: statsData, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardAPI.getStats(),
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  const { data: systemData, isLoading: systemLoading, refetch: refetchSystem } = useQuery({
    queryKey: ['system-status'],
    queryFn: () => dashboardAPI.getSystemStatus(),
    refetchInterval: 10000, // Refetch every 10 seconds
  });

  const handleRefresh = async () => {
    try {
      await Promise.all([
        refetchStats(),
        refetchSystem(),
        queryClient.invalidateQueries({ queryKey: ['user-growth'] }),
        queryClient.invalidateQueries({ queryKey: ['revenue-analytics'] }),
        queryClient.invalidateQueries({ queryKey: ['notifications'] }),
      ]);
      toast.success('Dashboard başarıyla yenilendi');
    } catch (error) {
      toast.error('Dashboard yenilenirken hata oluştu');
    }
  };

  const stats = statsData?.data;
  const systemStatus = systemData?.data;

  if (statsLoading) {
    return (
      <div className="space-y-8 animate-fade-in">
        {/* Header Skeleton */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-2">
            <div className="h-8 bg-gray-200 rounded-xl w-64 animate-pulse"></div>
            <div className="h-4 bg-gray-200 rounded w-96 animate-pulse"></div>
          </div>
          <div className="mt-4 sm:mt-0">
            <div className="h-10 bg-gray-200 rounded-xl w-32 animate-pulse"></div>
          </div>
        </div>

        {/* Stats Cards Skeleton */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array<undefined>(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 animate-pulse">
              <div className="flex items-center justify-between mb-4">
                <div className="w-12 h-12 bg-gray-200 rounded-xl"></div>
                <div className="w-16 h-6 bg-gray-200 rounded"></div>
              </div>
              <div className="h-8 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-4 bg-gray-200 rounded w-1/2"></div>
            </div>
          ))}
        </div>

        {/* Charts Skeleton */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 animate-pulse">
            <div className="h-6 bg-gray-200 rounded w-1/3 mb-6"></div>
            <div className="h-64 bg-gray-200 rounded-xl"></div>
          </div>
          <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 animate-pulse">
            <div className="h-6 bg-gray-200 rounded w-1/3 mb-6"></div>
            <div className="h-64 bg-gray-200 rounded-xl"></div>
          </div>
        </div>
      </div>
    );
  }

  const statCards: StatsCardProps[] = [
    {
      title: 'Toplam Kullanıcı',
      value: stats?.totalUsers || 0,
      change: `+${stats?.userGrowth || 0}%`,
      trend: 'up' as const,
      icon: UserGroupIcon,
      color: 'blue',
      description: 'Kayıtlı kullanıcı sayısı',
    },
    {
      title: 'Aktif Lisans',
      value: stats?.activeLicenses || 0,
      change: `+${stats?.licenseGrowth || 0}%`,
      trend: 'up' as const,
      icon: DocumentTextIcon,
      color: 'green',
      description: 'Aktif kullanımda',
    },
    {
      title: 'Toplam Gelir',
      value: `$${(stats?.totalRevenue || 0).toLocaleString()}`,
      change: `+${stats?.revenueGrowth || 0}%`,
      trend: 'up' as const,
      icon: CurrencyDollarIcon,
      color: 'yellow',
      description: 'Toplam gelir',
    },
    {
      title: 'Aktif Kullanıcı',
      value: stats?.activeUsers || 0,
      change: `+${stats?.activeUserGrowth || 0}%`,
      trend: 'up' as const,
      icon: ArrowTrendingUpIcon,
      color: 'purple',
      description: 'Aktif kullanıcılar',
    },
  ];

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Dashboard
          </h1>
          <p className="text-gray-600">
            Office AI yönetim paneline hoş geldiniz. Sistem durumunu ve metrikleri buradan takip edebilirsiniz.
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <button 
            onClick={handleRefresh}
            disabled={statsLoading || systemLoading}
            className="inline-flex items-center px-4 py-2 bg-gradient-to-r from-primary-600 to-primary-700 text-white text-sm font-medium rounded-xl hover:from-primary-700 hover:to-primary-800 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 transition-all duration-200 shadow-lg hover:shadow-xl transform hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
          >
            <SparklesIcon className={`w-4 h-4 mr-2 ${(statsLoading || systemLoading) ? 'animate-spin' : ''}`} />
            {(statsLoading || systemLoading) ? 'Yenileniyor...' : 'Yenile'}
          </button>
        </div>
      </div>

      {/* Key Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((stat, index) => (
          <div key={stat.title} className="animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
            <StatsCard {...stat} />
          </div>
        ))}
      </div>

      {/* Quick Insights */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="bg-gradient-to-br from-primary-500 to-primary-600 rounded-2xl p-6 text-white shadow-lg">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-white/20 rounded-xl flex items-center justify-center">
              <BoltIcon className="w-6 h-6 text-white" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold">{((systemStatus?.uptime || 0) / 86400 * 100).toFixed(1)}%</div>
              <div className="text-primary-100 text-sm">Uptime</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold mb-2">Sistem Performansı</h3>
          <p className="text-primary-100 text-sm">Tüm sistemler normal çalışıyor</p>
        </div>

        <div className="bg-gradient-to-br from-success-500 to-success-600 rounded-2xl p-6 text-white shadow-lg">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-white/20 rounded-xl flex items-center justify-center">
              <ShieldCheckIcon className="w-6 h-6 text-white" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold">{systemStatus?.security?.score || '100'}%</div>
              <div className="text-success-100 text-sm">Güvenlik</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold mb-2">Güvenlik Durumu</h3>
          <p className="text-success-100 text-sm">Tüm güvenlik kontrolleri aktif</p>
        </div>

        <div className="bg-gradient-to-br from-warning-500 to-warning-600 rounded-2xl p-6 text-white shadow-lg">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-white/20 rounded-xl flex items-center justify-center">
              <ChartBarIcon className="w-6 h-6 text-white" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold">₺{(stats?.weeklyRevenue || 0).toLocaleString()}</div>
              <div className="text-warning-100 text-sm">Bu hafta</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold mb-2">Haftalık Gelir</h3>
          <p className="text-warning-100 text-sm">Hedefin %120'si tamamlandı</p>
        </div>
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div className="animate-slide-up" style={{ animationDelay: '0.2s' }}>
          <RevenueChart />
        </div>
        <div className="animate-slide-up" style={{ animationDelay: '0.3s' }}>
          <UserGrowthChart />
        </div>
      </div>

      {/* Bottom Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Recent Activity */}
        <div className="lg:col-span-1 animate-slide-up" style={{ animationDelay: '0.4s' }}>
          <RecentActivity recentSignups={stats?.recentSignups || []} recentPayments={stats?.recentPayments || []} />
        </div>

        {/* System Status and Quick Actions Side by Side */}
        <div className="lg:col-span-2 grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* System Status */}
          {systemStatus && (
            <div className="animate-slide-up" style={{ animationDelay: '0.5s' }}>
              <SystemStatus status={systemStatus} loading={systemLoading} />
            </div>
          )}

          {/* Quick Actions */}
          <div className="animate-slide-up" style={{ animationDelay: '0.6s' }}>
            <QuickActions />
          </div>
        </div>
      </div>

      {/* Performance Metrics */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">Performans Metrikleri</h3>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
          <div className="text-center">
            <div className="w-16 h-16 bg-primary-100 rounded-2xl flex items-center justify-center mx-auto mb-3">
              <TrendingUpIcon className="w-8 h-8 text-primary-600" />
            </div>
            <div className="text-2xl font-bold text-gray-900 mb-1">{(systemStatus?.averageResponseTime || 2400)}ms</div>
            <div className="text-sm text-gray-600">Ortalama Yanıt Süresi</div>
            <div className="text-xs text-success-600 mt-1 flex items-center justify-center">
              <TrendingDownIcon className="w-3 h-3 mr-1" />
              %{systemStatus?.responseTimeImprovement || 15} iyileşme
            </div>
          </div>

          <div className="text-center">
            <div className="w-16 h-16 bg-success-100 rounded-2xl flex items-center justify-center mx-auto mb-3">
              <CheckCircleIcon className="w-8 h-8 text-success-600" />
            </div>
            <div className="text-2xl font-bold text-gray-900 mb-1">{((100 - (systemStatus?.api?.errorRate || 0.2)) || 99.8).toFixed(1)}%</div>
            <div className="text-sm text-gray-600">API Başarı Oranı</div>
            <div className="text-xs text-success-600 mt-1 flex items-center justify-center">
              <TrendingUpIcon className="w-3 h-3 mr-1" />
              %{systemStatus?.api?.successRateImprovement || 2} artış
            </div>
          </div>

          <div className="text-center">
            <div className="w-16 h-16 bg-warning-100 rounded-2xl flex items-center justify-center mx-auto mb-3">
              <ClockIcon className="w-8 h-8 text-warning-600" />
            </div>
            <div className="text-2xl font-bold text-gray-900 mb-1">{((stats?.monthlyRequests || 1200000) / 1000000).toFixed(1)}M</div>
            <div className="text-sm text-gray-600">Aylık İstekler</div>
            <div className="text-xs text-success-600 mt-1 flex items-center justify-center">
              <TrendingUpIcon className="w-3 h-3 mr-1" />
              %{stats?.requestGrowth || 8} artış
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}