import { 
  CheckCircleIcon, 
  ExclamationTriangleIcon, 
  XCircleIcon,
  ClockIcon 
} from '@heroicons/react/24/outline';
import type { SystemStatus as SystemStatusType } from '../../types';
import { clsx } from 'clsx';

interface SystemStatusProps {
  status: SystemStatusType;
  loading: boolean;
}

export function SystemStatus({ status, loading }: SystemStatusProps) {
  if (loading) {
    return (
      <div className="bg-white rounded-xl p-4 shadow-md border border-gray-200 animate-pulse">
        <div className="flex items-center space-x-3">
          <div className="w-5 h-5 bg-gray-200 rounded"></div>
          <div className="h-4 bg-gray-200 rounded w-32"></div>
        </div>
      </div>
    );
  }

  const getStatusConfig = (statusValue: string) => {
    switch (statusValue) {
      case 'healthy':
        return {
          icon: CheckCircleIcon,
          color: 'text-green-600',
          bg: 'bg-green-50',
          border: 'border-green-200',
          text: 'Sağlıklı',
        };
      case 'degraded':
        return {
          icon: ExclamationTriangleIcon,
          color: 'text-yellow-600',
          bg: 'bg-yellow-50',
          border: 'border-yellow-200',
          text: 'Performans Düşük',
        };
      case 'down':
        return {
          icon: XCircleIcon,
          color: 'text-red-600',
          bg: 'bg-red-50',
          border: 'border-red-200',
          text: 'Çevrimdışı',
        };
      default:
        return {
          icon: ClockIcon,
          color: 'text-gray-600',
          bg: 'bg-gray-50',
          border: 'border-gray-200',
          text: 'Bilinmiyor',
        };
    }
  };

  const webApiConfig = getStatusConfig(status.webApiStatus);
  const dbConfig = getStatusConfig(status.databaseStatus);

  const formatUptime = (seconds: number) => {
    const days = Math.floor(seconds / (24 * 3600));
    const hours = Math.floor((seconds % (24 * 3600)) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    
    if (days > 0) return `${days}g ${hours}s`;
    if (hours > 0) return `${hours}s ${minutes}d`;
    return `${minutes}d`;
  };

  const overallHealthy = status.webApiStatus === 'healthy' && status.databaseStatus === 'healthy';

  return (
    <div className={clsx(
      'rounded-xl p-4 shadow-md border transition-all',
      overallHealthy 
        ? 'bg-green-50 border-green-200' 
        : 'bg-yellow-50 border-yellow-200'
    )}>
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          {/* Overall Status */}
          <div className="flex items-center space-x-2">
            {overallHealthy ? (
              <CheckCircleIcon className="w-5 h-5 text-green-600" />
            ) : (
              <ExclamationTriangleIcon className="w-5 h-5 text-yellow-600" />
            )}
            <span className="font-medium text-gray-900">
              {overallHealthy ? 'Sistem Normal' : 'Sistem Uyarısı'}
            </span>
          </div>

          {/* Individual Status */}
          <div className="flex items-center space-x-6 text-sm">
            <div className="flex items-center space-x-1">
              <webApiConfig.icon className={clsx('w-4 h-4', webApiConfig.color)} />
              <span className="text-gray-700">WebAPI: {webApiConfig.text}</span>
            </div>
            <div className="flex items-center space-x-1">
              <dbConfig.icon className={clsx('w-4 h-4', dbConfig.color)} />
              <span className="text-gray-700">Veritabanı: {dbConfig.text}</span>
            </div>
          </div>
        </div>

        {/* System Info */}
        <div className="flex items-center space-x-4 text-sm text-gray-600">
          <div className="flex items-center space-x-1">
            <ClockIcon className="w-4 h-4" />
            <span>Çalışma süresi: {formatUptime(status.uptime)}</span>
          </div>
          <div className="bg-gray-100 px-2 py-1 rounded text-xs font-mono">
            v{status.version}
          </div>
        </div>
      </div>

      {/* Warning Message */}
      {!overallHealthy && (
        <div className="mt-3 text-sm text-yellow-800">
          <p>
            Sistem performansında düşüş tespit edildi. Lütfen sistem yöneticisine başvurun.
          </p>
        </div>
      )}
    </div>
  );
}