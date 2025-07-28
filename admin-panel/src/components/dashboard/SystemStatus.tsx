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
      <div className="bg-white rounded-xl p-6 shadow-md border border-gray-200">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Sistem Durumu</h3>
        <div className="space-y-4 animate-pulse">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="flex items-center space-x-3">
              <div className="w-5 h-5 bg-gray-200 rounded"></div>
              <div className="h-4 bg-gray-200 rounded w-32"></div>
              <div className="h-3 bg-gray-200 rounded w-16 ml-auto"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!status) {
    return (
      <div className="bg-white rounded-xl p-6 shadow-md border border-gray-200">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Sistem Durumu</h3>
        <div className="text-center py-8 text-gray-500">
          <p>Sistem durumu yüklenemedi</p>
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

  const services = [
    { name: 'Veritabanı', ...status.database },
    { name: 'API', ...status.api },
    { name: 'Depolama', ...status.storage },
    { name: 'Bellek', ...status.memory },
  ];

  const allHealthy = services.every(service => service.status === 'healthy');

  return (
    <div className="bg-white rounded-xl p-6 shadow-md border border-gray-200">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Sistem Durumu</h3>
        <div className={clsx(
          'px-3 py-1 rounded-full text-sm font-medium',
          allHealthy
            ? 'bg-green-100 text-green-800'
            : 'bg-yellow-100 text-yellow-800'
        )}>
          {allHealthy ? 'Tüm Sistemler Çalışıyor' : 'Dikkat Gerekiyor'}
        </div>
      </div>

      <div className="space-y-3">
        {services.map((service) => {
          const config = getStatusConfig(service.status);
          const Icon = config.icon;

          return (
            <div key={service.name} className="flex items-center justify-between p-3 rounded-lg border border-gray-100">
              <div className="flex items-center space-x-3">
                <Icon className={clsx('w-5 h-5', config.color)} />
                <span className="font-medium text-gray-900">{service.name}</span>
              </div>
              <div className="flex items-center space-x-3">
                {service.responseTime && (
                  <span className="text-sm text-gray-500">{service.responseTime}</span>
                )}
                {service.usage && (
                  <span className="text-sm text-gray-500">{service.usage}</span>
                )}
                <span className={clsx('text-sm font-medium', config.color)}>
                  {config.text}
                </span>
              </div>
            </div>
          );
        })}
      </div>

      <div className="mt-4 pt-4 border-t border-gray-200">
        <p className="text-xs text-gray-500">
          Son güncelleme: {new Date(status.lastUpdated).toLocaleString('tr-TR')}
        </p>
      </div>
    </div>
  );
}