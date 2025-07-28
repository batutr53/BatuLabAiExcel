import React, { useState } from 'react';
import { User, Payment } from '../../types';
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
    <div className="bg-white rounded-lg shadow-sm border border-gray-200">
      {/* Header with Tabs */}
      <div className="border-b border-gray-200">
        <div className="px-6 py-4">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Son Aktiviteler</h2>
          <div className="flex space-x-4">
            <button
              onClick={() => setActiveTab('signups')}
              className={clsx(
                'flex items-center space-x-2 px-3 py-2 text-sm font-medium rounded-lg transition-colors',
                activeTab === 'signups'
                  ? 'bg-primary-100 text-primary-700'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              )}
            >
              <UserIcon className="w-4 h-4" />
              <span>Yeni Kayıtlar ({recentSignups.length})</span>
            </button>
            <button
              onClick={() => setActiveTab('payments')}
              className={clsx(
                'flex items-center space-x-2 px-3 py-2 text-sm font-medium rounded-lg transition-colors',
                activeTab === 'payments'
                  ? 'bg-primary-100 text-primary-700'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'
              )}
            >
              <CreditCardIcon className="w-4 h-4" />
              <span>Son Ödemeler ({recentPayments.length})</span>
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="p-6">
        {activeTab === 'signups' && (
          <div className="space-y-4">
            {recentSignups.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <UserIcon className="w-12 h-12 mx-auto mb-3 opacity-50" />
                <p>Henüz yeni kayıt yok</p>
              </div>
            ) : (
              recentSignups.map((user) => (
                <div
                  key={user.id}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                >
                  <div className="flex items-center space-x-3">
                    <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
                      <UserIcon className="w-5 h-5 text-primary-600" />
                    </div>
                    <div>
                      <p className="font-medium text-gray-900">
                        {user.name || user.email}
                      </p>
                      <p className="text-sm text-gray-600">{user.email}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-3">
                    <div className="text-right">
                      <p className="text-sm text-gray-600">
                        {formatDistanceToNow(new Date(user.createdAt), {
                          addSuffix: true,
                          locale: tr,
                        })}
                      </p>
                      <div className="flex items-center space-x-1 mt-1">
                        <div
                          className={clsx(
                            'w-2 h-2 rounded-full',
                            user.isActive ? 'bg-green-400' : 'bg-gray-400'
                          )}
                        />
                        <span className="text-xs text-gray-500">
                          {user.isActive ? 'Aktif' : 'Pasif'}
                        </span>
                      </div>
                    </div>
                    <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-white rounded-lg">
                      <EyeIcon className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === 'payments' && (
          <div className="space-y-4">
            {recentPayments.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <CreditCardIcon className="w-12 h-12 mx-auto mb-3 opacity-50" />
                <p>Henüz ödeme yok</p>
              </div>
            ) : (
              recentPayments.map((payment) => {
                const statusConfig = getPaymentStatusConfig(payment.status);
                return (
                  <div
                    key={payment.id}
                    className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                  >
                    <div className="flex items-center space-x-3">
                      <div className={clsx('w-10 h-10 rounded-full flex items-center justify-center', statusConfig.bg)}>
                        <statusConfig.icon className={clsx('w-5 h-5', statusConfig.color)} />
                      </div>
                      <div>
                        <p className="font-medium text-gray-900">
                          ₺{payment.amount.toLocaleString()} {payment.currency}
                        </p>
                        <p className="text-sm text-gray-600">
                          {payment.paymentMethod || 'Kredi Kartı'}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center space-x-3">
                      <div className="text-right">
                        <p className="text-sm text-gray-600">
                          {formatDistanceToNow(new Date(payment.createdAt), {
                            addSuffix: true,
                            locale: tr,
                          })}
                        </p>
                        <div className="flex items-center space-x-1 mt-1">
                          <span className={clsx('text-xs font-medium', statusConfig.color)}>
                            {statusConfig.text}
                          </span>
                        </div>
                      </div>
                      <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-white rounded-lg">
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