import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  BellIcon,
  CheckCircleIcon,
  EyeIcon,
  TrashIcon,
  PlusIcon,
  EnvelopeIcon
} from '@heroicons/react/24/outline';
import { notificationAPI } from '../../services/api';
import type { FilterState, Notification } from '../../types';
import { clsx } from 'clsx';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';
import toast from 'react-hot-toast';
import { Link } from 'react-router-dom';

export function NotificationsPage() {
  const [filters, setFilters] = useState<FilterState>({
    page: 1,
    pageSize: 10,
  });
  
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['notifications', filters],
    queryFn: () => notificationAPI.getNotifications(),
  });

  const markAsReadMutation = useMutation({
    mutationFn: (notificationId: string) => notificationAPI.markNotificationAsRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      toast.success('Bildirim okundu olarak işaretlendi!');
    },
    onError: (error) => {
      toast.error('Bildirim işaretlenirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const notifications = data?.data?.data || [];
  const totalCount = data?.data?.totalCount || 0;
  const totalPages = data?.data?.totalPages || 1;

  const handlePageChange = (page: number) => {
    setFilters(prev => ({ ...prev, page }));
  };

  const getNotificationTypeConfig = (type: string) => {
    switch (type) {
      case 'info':
        return { icon: BellIcon, color: 'text-blue-600', bg: 'bg-blue-100' };
      case 'success':
        return { icon: CheckCircleIcon, color: 'text-green-600', bg: 'bg-green-100' };
      case 'warning':
        return { icon: BellIcon, color: 'text-yellow-600', bg: 'bg-yellow-100' };
      case 'error':
        return { icon: BellIcon, color: 'text-red-600', bg: 'bg-red-100' };
      default:
        return { icon: BellIcon, color: 'text-gray-600', bg: 'bg-gray-100' };
    }
  };

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Bildirimler yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Bildirimler</h1>
          <p className="mt-2 text-gray-600">
            Toplam {totalCount} bildirim
          </p>
        </div>
        <div className="mt-4 sm:mt-0 flex space-x-3">
          <Link
            to="/notifications/send"
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
          >
            <EnvelopeIcon className="w-4 h-4 mr-2" />
            Bildirim Gönder
          </Link>
        </div>
      </div>

      {/* Notifications List */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-2 text-gray-500">Bildirimler yükleniyor...</p>
          </div>
        ) : notifications.length === 0 ? (
          <div className="p-8 text-center">
            <BellIcon className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-sm font-medium text-gray-900">Hiç bildirim yok</h3>
            <p className="mt-1 text-sm text-gray-500">Tüm bildirimleriniz burada görünecek.</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {notifications.map((notification: Notification) => {
              const config = getNotificationTypeConfig(notification.type);
              const Icon = config.icon;
              return (
                <div
                  key={notification.id}
                  className={clsx(
                    'flex items-center p-4 hover:bg-gray-50 transition-colors',
                    !notification.isRead && 'bg-blue-50'
                  )}
                >
                  <div className={clsx('flex-shrink-0 p-2 rounded-full', config.bg)}>
                    <Icon className={clsx('w-6 h-6', config.color)} />
                  </div>
                  <div className="ml-4 flex-1">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-medium text-gray-900">{notification.title}</p>
                      <p className="text-xs text-gray-500">
                        {formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true, locale: tr })}
                      </p>
                    </div>
                    <p className="text-sm text-gray-600 mt-1">{notification.message}</p>
                  </div>
                  <div className="ml-4 flex-shrink-0">
                    {!notification.isRead && (
                      <button
                        onClick={() => markAsReadMutation.mutate(notification.id)}
                        disabled={markAsReadMutation.isPending}
                        className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-full shadow-sm text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
                      >
                        <EyeIcon className="w-4 h-4 mr-1" />
                        Okundu
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && ( // Assuming totalPages is available from API response
          <div className="bg-white px-4 py-3 border-t border-gray-200 sm:px-6">
            <div className="flex items-center justify-between">
              <div className="flex-1 flex justify-between sm:hidden">
                <button
                  onClick={() => handlePageChange(filters.page - 1)}
                  disabled={filters.page <= 1}
                  className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Önceki
                </button>
                <button
                  onClick={() => handlePageChange(filters.page + 1)}
                  disabled={filters.page >= totalPages}
                  className="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Sonraki
                </button>
              </div>
              <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm text-gray-700">
                    <span className="font-medium">{((filters.page - 1) * filters.pageSize) + 1}</span>
                    {' '}-{' '}
                    <span className="font-medium">{Math.min(filters.page * filters.pageSize, totalCount)}</span>
                    {' '}arası, toplam{' '}
                    <span className="font-medium">{totalCount}</span>
                    {' '}sonuç
                  </p>
                </div>
                <div>
                  <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px">
                    <button
                      onClick={() => handlePageChange(filters.page - 1)}
                      disabled={filters.page <= 1}
                      className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Önceki
                    </button>
                    
                    {[...Array(Math.min(totalPages, 5))].map((_, index) => {
                      const page = index + 1;
                      return (
                        <button
                          key={page}
                          onClick={() => handlePageChange(page)}
                          className={clsx(
                            'relative inline-flex items-center px-4 py-2 border text-sm font-medium',
                            page === filters.page
                              ? 'z-10 bg-primary-50 border-primary-500 text-primary-600'
                              : 'bg-white border-gray-300 text-gray-500 hover:bg-gray-50'
                          )}
                        >
                          {page}
                        </button>
                      );
                    })}
                    
                    <button
                      onClick={() => handlePageChange(filters.page + 1)}
                      disabled={filters.page >= totalPages}
                      className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Sonraki
                    </button>
                  </nav>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
