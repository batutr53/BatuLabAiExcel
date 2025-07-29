import { useState } from 'react';
import type { User, Payment } from '../../types';
import { 
  UserIcon,
  CreditCardIcon,
  ClockIcon,
  EyeIcon,
  CheckCircleIcon,
  XCircleIcon
} from '@heroicons/react/24/outline';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';
import { clsx } from 'clsx';

interface RecentActivityProps {
  recentSignups: User[];
  recentPayments: Payment[];
}

type Tab = 'signups' | 'payments';

export function RecentActivity({ recentSignups, recentPayments }: RecentActivityProps) {
  const [activeTab, setActiveTab] = useState<Tab>('signups');

  const getPaymentStatusConfig = (status: string) => {
    switch (status) {
      case 'succeeded':
        return {
          icon: CheckCircleIcon,
          color: 'text-green-600',
          bg: 'bg-green-100',
          text: 'Başarılı',
        };
      case 'pending':
        return {
          icon: ClockIcon,
          color: 'text-yellow-600',
          bg: 'bg-yellow-100',
          text: 'Bekliyor',
        };
      case 'failed':
        return {
          icon: XCircleIcon,
          color: 'text-red-600',
          bg: 'bg-red-100',
          text: 'Başarısız',
        };
      default:
        return {
          icon: ClockIcon,
          color: 'text-gray-600',
          bg: 'bg-gray-100',
          text: status,
        };
    }
  };

  return (
    <div className="bg-white rounded-xl shadow-md border border-gray-200">
      {/* Header with Tabs */}
      <div className="border-b border-gray-200 p-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Son Aktiviteler</h2>
        <div className="flex space-x-4">
          <button
            onClick={() => setActiveTab('signups')}
            className={clsx(
              'flex items-center space-x-2 px-4 py-2 text-sm font-medium rounded-lg transition-colors',
              activeTab === 'signups'
                ? 'bg-primary-100 text-primary-700'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
            )}
          >
            <UserIcon className="w-5 h-5" />
            <span>Yeni Kayıtlar ({recentSignups.length})</span>
          </button>
          <button
            onClick={() => setActiveTab('payments')}
            className={clsx(
              'flex items-center space-x-2 px-4 py-2 text-sm font-medium rounded-lg transition-colors',
              activeTab === 'payments'
                ? 'bg-primary-100 text-primary-700'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
            )}
          >
            <CreditCardIcon className="w-5 h-5" />
            <span>Son Ödemeler ({recentPayments.length})</span>
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="p-6">
        {activeTab === 'signups' && (
          <div className="space-y-2">
            {recentSignups.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <UserIcon className="w-14 h-14 mx-auto mb-3 opacity-50" />
                <p className="text-lg">Henüz yeni kayıt yok</p>
              </div>
            ) : (
              recentSignups.map((user) => (
                <div
                  key={user.id}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                >
                  <div className="flex items-center space-x-3 flex-1 min-w-0">
                    <div className="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
                      <UserIcon className="w-4 h-4 text-primary-600" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center space-x-2">
                        <p className="font-medium text-gray-900 text-sm truncate">
                          {user.name || user.email}
                        </p>
                        <div
                          className={clsx(
                            'w-2 h-2 rounded-full flex-shrink-0',
                            user.isActive ? 'bg-green-400' : 'bg-gray-400'
                          )}
                        />
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2 flex-shrink-0">
                    <p className="text-xs text-gray-500">
                      {formatDistanceToNow(new Date(user.createdAt), {
                        addSuffix: true,
                        locale: tr,
                      })}
                    </p>
                    <button className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-white rounded">
                      <EyeIcon className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === 'payments' && (
          <div className="space-y-2">
            {recentPayments.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <CreditCardIcon className="w-14 h-14 mx-auto mb-3 opacity-50" />
                <p className="text-lg">Henüz ödeme yok</p>
              </div>
            ) : (
              recentPayments.map((payment) => {
                const statusConfig = getPaymentStatusConfig(payment.status);
                return (
                  <div
                    key={payment.id}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                  >
                    <div className="flex items-center space-x-3 flex-1 min-w-0">
                      <div className={clsx('w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0', statusConfig.bg)}>
                        <statusConfig.icon className={clsx('w-4 h-4', statusConfig.color)} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center space-x-2">
                          <p className="font-medium text-gray-900 text-sm">
                            ₺{payment.amount.toLocaleString()}
                          </p>
                          <span className={clsx('text-xs font-medium px-2 py-0.5 rounded-full', statusConfig.bg, statusConfig.color)}>
                            {statusConfig.text}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center space-x-2 flex-shrink-0">
                      <p className="text-xs text-gray-500">
                        {formatDistanceToNow(new Date(payment.createdAt), {
                          addSuffix: true,
                          locale: tr,
                        })}
                      </p>
                      <button className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-white rounded">
                        <EyeIcon className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        )}
      </div>
    </div>
  );
}
