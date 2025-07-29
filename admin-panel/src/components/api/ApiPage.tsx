import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  CodeBracketIcon,
  ChartBarIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon,
  ArrowPathIcon,
  EyeIcon,
  DocumentTextIcon
} from '@heroicons/react/24/outline';
import { dashboardAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';

interface ApiEndpoint {
  method: string;
  path: string;
  status: 'healthy' | 'degraded' | 'down';
  responseTime: number;
  requestCount: number;
  errorRate: number;
  lastError?: string;
}

interface ApiMetrics {
  totalRequests: number;
  averageResponseTime: number;
  errorRate: number;
  activeConnections: number;
  endpoints: ApiEndpoint[];
  rateLimits: {
    current: number;
    limit: number;
    resetTime: string;
  };
  lastUpdated: string;
}

export function ApiPage() {
  const [selectedEndpoint, setSelectedEndpoint] = useState<ApiEndpoint | null>(null);
  
  const { data: apiData, isLoading, refetch } = useQuery({
    queryKey: ['api-metrics'],
    queryFn: () => dashboardAPI.getApiMetrics(),
    refetchInterval: 10000, // Refetch every 10 seconds
  });

  const metrics = apiData?.data as ApiMetrics;

  const handleRefresh = async () => {
    try {
      await refetch();
      toast.success('API metrikleri yenilendi');
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
          text: 'Yavaş',
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

  const getMethodColor = (method: string) => {
    switch (method.toUpperCase()) {
      case 'GET':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400';
      case 'POST':
        return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400';
      case 'PUT':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400';
      case 'DELETE':
        return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400';
    }
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
            API Durumu
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            API endpoint'lerini ve performans metriklerini gerçek zamanlı takip edin.
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <button 
            onClick={handleRefresh}
            disabled={isLoading}
            className="inline-flex items-center px-4 py-2 bg-gradient-to-r from-primary-600 to-primary-700 text-white text-sm font-medium rounded-xl hover:from-primary-700 hover:to-primary-800 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 transition-all duration-200 shadow-lg hover:shadow-xl transform hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
          >
            <ArrowPathIcon className={`w-4 h-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
            {isLoading ? 'Yenileniyor...' : 'Yenile'}
          </button>
        </div>
      </div>

      {/* API Overview Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-4 gap-6">
        {/* Total Requests */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-blue-100 dark:bg-blue-900/30 rounded-xl flex items-center justify-center">
              <ChartBarIcon className="w-6 h-6 text-blue-600 dark:text-blue-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {(metrics?.totalRequests || 0).toLocaleString()}
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Bu ay</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Toplam İstek</h3>
        </div>

        {/* Average Response Time */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-green-100 dark:bg-green-900/30 rounded-xl flex items-center justify-center">
              <ClockIcon className="w-6 h-6 text-green-600 dark:text-green-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.averageResponseTime || 0}ms
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Ortalama</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Yanıt Süresi</h3>
        </div>

        {/* Error Rate */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-red-100 dark:bg-red-900/30 rounded-xl flex items-center justify-center">
              <ExclamationTriangleIcon className="w-6 h-6 text-red-600 dark:text-red-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {(metrics?.errorRate || 0).toFixed(2)}%
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Hata oranı</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Hata Durumu</h3>
        </div>

        {/* Active Connections */}
        <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
          <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 bg-purple-100 dark:bg-purple-900/30 rounded-xl flex items-center justify-center">
              <CodeBracketIcon className="w-6 h-6 text-purple-600 dark:text-purple-400" />
            </div>
            <div className="text-right">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {metrics?.activeConnections || 0}
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Şu anda</div>
            </div>
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Aktif Bağlantı</h3>
        </div>
      </div>

      {/* Rate Limits */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-6">Rate Limiting</h2>
        <div className="flex items-center justify-between mb-4">
          <div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Mevcut Kullanım</div>
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {metrics?.rateLimits.current || 0} / {metrics?.rateLimits.limit || 0}
            </div>
          </div>
          <div className="text-right">
            <div className="text-sm text-gray-600 dark:text-gray-400">Sıfırlanma Zamanı</div>
            <div className="text-lg font-semibold text-gray-900 dark:text-white">
              {new Date(metrics?.rateLimits.resetTime || Date.now()).toLocaleTimeString('tr-TR')}
            </div>
          </div>
        </div>
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3">
          <div 
            className={clsx(
              'h-3 rounded-full transition-all duration-500',
              (metrics?.rateLimits.current || 0) / (metrics?.rateLimits.limit || 1) > 0.8
                ? 'bg-red-600'
                : (metrics?.rateLimits.current || 0) / (metrics?.rateLimits.limit || 1) > 0.6
                ? 'bg-yellow-600'
                : 'bg-green-600'
            )}
            style={{ 
              width: `${((metrics?.rateLimits.current || 0) / (metrics?.rateLimits.limit || 1)) * 100}%` 
            }}
          ></div>
        </div>
      </div>

      {/* API Endpoints */}
      <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">API Endpoint'leri</h2>
        </div>
        <div className="divide-y divide-gray-200 dark:divide-gray-700">
          {metrics?.endpoints?.map((endpoint, index) => {
            const config = getStatusConfig(endpoint.status);
            const Icon = config.icon;
            
            return (
              <div 
                key={index} 
                className="p-6 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors cursor-pointer"
                onClick={() => setSelectedEndpoint(endpoint)}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4 flex-1 min-w-0">
                    <Icon className={clsx('w-5 h-5 flex-shrink-0', config.color)} />
                    <div className="flex items-center space-x-3 flex-1 min-w-0">
                      <span className={clsx('px-2 py-1 text-xs font-medium rounded-full flex-shrink-0', getMethodColor(endpoint.method))}>
                        {endpoint.method}
                      </span>
                      <span className="font-mono text-sm text-gray-900 dark:text-white truncate">
                        {endpoint.path}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center space-x-6 text-sm text-gray-600 dark:text-gray-400 flex-shrink-0">
                    <div className="text-center">
                      <div className="font-semibold text-gray-900 dark:text-white">{endpoint.responseTime}ms</div>
                      <div>Yanıt</div>
                    </div>
                    <div className="text-center">
                      <div className="font-semibold text-gray-900 dark:text-white">{endpoint.requestCount.toLocaleString()}</div>
                      <div>İstek</div>
                    </div>
                    <div className="text-center">
                      <div className={clsx('font-semibold', endpoint.errorRate > 5 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white')}>
                        {endpoint.errorRate.toFixed(1)}%
                      </div>
                      <div>Hata</div>
                    </div>
                    <button className="p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-white dark:hover:bg-gray-800 rounded">
                      <EyeIcon className="w-4 h-4" />
                    </button>
                  </div>
                </div>
                {endpoint.lastError && (
                  <div className="mt-3 p-2 bg-red-50 dark:bg-red-900/20 rounded-lg">
                    <div className="flex items-center space-x-2">
                      <ExclamationTriangleIcon className="w-4 h-4 text-red-600 dark:text-red-400 flex-shrink-0" />
                      <span className="text-sm text-red-800 dark:text-red-200 truncate">
                        Son hata: {endpoint.lastError}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* API Documentation Link */}
      <div className="bg-gradient-to-r from-primary-50 to-primary-100 dark:from-primary-900/20 dark:to-primary-800/20 rounded-2xl p-6 border border-primary-200 dark:border-primary-800">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 bg-primary-100 dark:bg-primary-900/30 rounded-xl flex items-center justify-center">
              <DocumentTextIcon className="w-6 h-6 text-primary-600 dark:text-primary-400" />
            </div>
            <div>
              <h3 className="text-lg font-semibold text-primary-900 dark:text-primary-100">API Dokümantasyonu</h3>
              <p className="text-primary-700 dark:text-primary-300">Detaylı API rehberi ve örnekler için dokümantasyonu inceleyin.</p>
            </div>
          </div>
          <button className="px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium rounded-lg transition-colors">
            Dokümantasyonu Görüntüle
          </button>
        </div>
      </div>
    </div>
  );
}