import { useQuery } from '@tanstack/react-query';
import { 
  ServerIcon,
  CpuChipIcon,
  CircleStackIcon,
  CloudIcon,
  WifiIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon,
  ClockIcon
} from '@heroicons/react/24/outline';
import { dashboardAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';

interface SystemMetrics {
  cpu: {
    usage: number;
    cores: number;
    temperature?: number;
  };
  memory: {
    used: number;
    total: number;
    percentage: number;
  };
  disk: {
    used: number;
    total: number;
    percentage: number;
  };
  network: {
    upload: number;
    download: number;
    latency: number;
  };
  services: {
    database: { status: string; responseTime: string };
    api: { status: string; responseTime: string };
    storage: { status: string; responseTime: string };
    cache: { status: string; responseTime: string };
  };
  uptime: number;
  lastUpdated: string;
}

export function SystemPage() {
  const { data: systemData, isLoading, refetch } = useQuery({
    queryKey: ['system-metrics'],
    queryFn: () => dashboardAPI.getSystemMetrics(),
    refetchInterval: 5000, // Refetch every 5 seconds
  });

  const metrics = systemData?.data as SystemMetrics;

  const handleRefresh = async () => {
    try {
      await refetch();
      toast.success('Sistem metrikleri yenilendi');
    } catch (error) {
      toast.error('Metrikler yenilenirken hata oluştu');
    }
  };

  const getStatusConfig = (status: string) => {
    switch (status) {
      case 'healthy':
        return {
          icon: CheckCircleIcon,
          color: 'text-green-600 dark:text-green-400',
          bg: 'bg-green-50 dark:bg-green-900/20',
          border: 'border-green-200 dark:border-green-800',
          text: 'Sağlıklı',
        };
      case 'degraded':
        return {
          icon: ExclamationTriangleIcon,
          color: 'text-yellow-600 dark:text-yellow-400',
          bg: 'bg-yellow-50 dark:bg-yellow-900/20',
          border: 'border-yellow-200 dark:border-yellow-800',
          text: 'Performans Düşük',
        };
      case 'down':
        return {
          icon: XCircleIcon,
          color: 'text-red-600 dark:text-red-400',
          bg: 'bg-red-50 dark:bg-red-900/20',
          border: 'border-red-200 dark:border-red-800',
          text: 'Çevrimdışı',
        };
      default:
        return {
          icon: ClockIcon,
          color: 'text-gray-600 dark:text-gray-400',
          bg: 'bg-gray-50 dark:bg-gray-900/20',
          border: 'border-gray-200 dark:border-gray-700',
          text: 'Bilinmiyor',
        };
    }
  };

  const formatUptime = (seconds: number) => {
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${days}g ${hours}s ${minutes}d`;
  };

  if (isLoading) {
    return (
      <div className="space-y-8 animate-fade-in">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-2">
            <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded-xl w-64 animate-pulse"></div>
            <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-96 animate-pulse"></div>
          </div>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700 animate-pulse">
              <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-4"></div>
              <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2"></div>
              <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/3"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            Sistem Durumu
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Sistem kaynaklarını ve servis durumunu gerçek zamanlı takip edin.
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <button 
            onClick={handleRefresh}
            disabled={isLoading}
            className="inline-flex items-center px-4 py-2 bg-gradient-to-r from-primary-600 to-primary-700 text-white text-sm font-medium rounded-xl hover:from-primary-700 hover:to-primary-800 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 transition-all duration-200 shadow-lg hover:shadow-xl transform hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
          >
            <ServerIcon className={`w-4 h-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
            {isLoading ? 'Yenileniyor...' : 'Yenile'}
          </button>
        </div>
      </div>

      {/* System Overview Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-4 gap-6">
        {/* CPU Usage */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-blue-100 dark:bg-blue-900/30 rounded-xl flex items-center justify-center">
              <CpuChipIcon className="w-6 h-6 text-blue-600 dark:text-blue-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.cpu.usage || 0}%
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">
                {metrics?.cpu.cores || 0} Çekirdek
              </div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">CPU Kullanımı</h3>
          <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
            <div 
              className="bg-blue-600 h-2 rounded-full transition-all duration-500"
              style={{ width: `${metrics?.cpu.usage || 0}%` }}
            ></div>
          </div>
        </div>

        {/* Memory Usage */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-green-100 dark:bg-green-900/30 rounded-xl flex items-center justify-center">
              <CircleStackIcon className="w-6 h-6 text-green-600 dark:text-green-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.memory.percentage || 0}%
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">
                {((metrics?.memory.used || 0) / 1024).toFixed(1)} GB / {((metrics?.memory.total || 0) / 1024).toFixed(1)} GB
              </div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Bellek Kullanımı</h3>
          <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
            <div 
              className="bg-green-600 h-2 rounded-full transition-all duration-500"
              style={{ width: `${metrics?.memory.percentage || 0}%` }}
            ></div>
          </div>
        </div>

        {/* Disk Usage */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-yellow-100 dark:bg-yellow-900/30 rounded-xl flex items-center justify-center">
              <CloudIcon className="w-6 h-6 text-yellow-600 dark:text-yellow-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.disk.percentage || 0}%
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">
                {((metrics?.disk.used || 0) / 1024).toFixed(1)} GB / {((metrics?.disk.total || 0) / 1024).toFixed(1)} GB
              </div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Disk Kullanımı</h3>
          <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
            <div 
              className="bg-yellow-600 h-2 rounded-full transition-all duration-500"
              style={{ width: `${metrics?.disk.percentage || 0}%` }}
            ></div>
          </div>
        </div>

        {/* Network Status */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-purple-100 dark:bg-purple-900/30 rounded-xl flex items-center justify-center">
              <WifiIcon className="w-6 h-6 text-purple-600 dark:text-purple-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.network.latency || 0}ms
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Gecikme</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Ağ Durumu</h3>
          <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400">
            <span>↑ {(metrics?.network.upload || 0).toFixed(1)} MB/s</span>
            <span>↓ {(metrics?.network.download || 0).toFixed(1)} MB/s</span>
          </div>
        </div>
      </div>

      {/* Services Status */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-6">Servis Durumu</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {metrics?.services && Object.entries(metrics.services).map(([serviceName, service]) => {
            const config = getStatusConfig(service.status);
            const Icon = config.icon;
            
            return (
              <div key={serviceName} className={clsx('p-4 rounded-xl border-2 transition-all', config.bg, config.border)}>
                <div className="flex items-center justify-between mb-3">
                  <Icon className={clsx('w-6 h-6', config.color)} />
                  <span className={clsx('text-xs font-medium px-2 py-1 rounded-full', config.bg, config.color)}>
                    {config.text}
                  </span>
                </div>
                <h3 className="font-semibold text-gray-900 dark:text-white capitalize">{serviceName}</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Yanıt: {service.responseTime}
                </p>
              </div>
            );
          })}
        </div>
      </div>

      {/* System Information */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-6">Sistem Bilgileri</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="text-center">
            <div className="text-3xl font-bold text-primary-600 dark:text-primary-400 mb-2">
              {formatUptime(metrics?.uptime || 0)}
            </div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Sistem Çalışma Süresi</div>
          </div>
          <div className="text-center">
            <div className="text-3xl font-bold text-success-600 dark:text-success-400 mb-2">
              99.9%
            </div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Kullanılabilirlik</div>
          </div>
          <div className="text-center">
            <div className="text-3xl font-bold text-blue-600 dark:text-blue-400 mb-2">
              {new Date(metrics?.lastUpdated || Date.now()).toLocaleTimeString('tr-TR')}
            </div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Son Güncelleme</div>
          </div>
        </div>
      </div>
    </div>
  );
}