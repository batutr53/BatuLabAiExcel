import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  UserIcon,
  MagnifyingGlassIcon,
  FunnelIcon,
  EllipsisVerticalIcon,
  CheckCircleIcon,
  XCircleIcon,
  PlusIcon,
  TrashIcon,
  PencilIcon
} from '@heroicons/react/24/outline';
import { userAPI } from '../../services/api';
import type { FilterState } from '../../types';
import { clsx } from 'clsx';
import { ConfirmModal } from '../licenses/ConfirmModal'; // Reusing ConfirmModal
import { UserEditModal } from './UserEditModal'; // New component for editing
import toast from 'react-hot-toast';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  licenseCount: number;
  activeLicense?: {
    id: string;
    type: number;
    licenseKey: string;
    expiresAt?: string;
  };
}

export function UsersPage() {
  const [filters, setFilters] = useState<FilterState>({
    search: '',
    page: 1,
    pageSize: 10,
  });
  
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null);
  const [editModal, setEditModal] = useState<{ isOpen: boolean; user?: User }>({ isOpen: false });
  const [confirmModal, setConfirmModal] = useState<{
    isOpen: boolean;
    type: 'suspend' | 'unsuspend' | 'delete';
    userId?: string;
    userName?: string;
  }>({ isOpen: false, type: 'suspend' });
  
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['users', filters],
    queryFn: () => userAPI.getUsers(filters),
  });

  const suspendMutation = useMutation({
    mutationFn: (userId: string) => userAPI.suspendUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setActiveDropdown(null);
      toast.success('Kullanıcı başarıyla askıya alındı!');
    },
    onError: (error) => {
      toast.error('Kullanıcı askıya alınırken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const unsuspendMutation = useMutation({
    mutationFn: (userId: string) => userAPI.unsuspendUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setActiveDropdown(null);
      toast.success('Kullanıcı başarıyla aktifleştirildi!');
    },
    onError: (error) => {
      toast.error('Kullanıcı aktifleştirilirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<User> }) => userAPI.updateUser(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setEditModal({ isOpen: false });
      toast.success('Kullanıcı başarıyla güncellendi!');
    },
    onError: (error) => {
      toast.error('Kullanıcı güncellenirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (userId: string) => userAPI.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setActiveDropdown(null);
      toast.success('Kullanıcı başarıyla silindi!');
    },
    onError: (error) => {
      toast.error('Kullanıcı silinirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const users = data?.data?.data || [];
  const totalCount = data?.data?.totalCount || 0;
  const totalPages = data?.data?.totalPages || 1;

  const handleSearch = (search: string) => {
    setFilters(prev => ({ ...prev, search, page: 1 }));
  };

  const handleStatusFilter = (isActive?: boolean) => {
    setFilters(prev => ({ ...prev, isActive, page: 1 }));
  };

  const handlePageChange = (page: number) => {
    setFilters(prev => ({ ...prev, page }));
  };

  const handleUserAction = (user: User, action: 'suspend' | 'unsuspend' | 'delete') => {
    setConfirmModal({
      isOpen: true,
      type: action,
      userId: user.id,
      userName: user.fullName,
    });
    setActiveDropdown(null);
  };

  const handleEditUser = (user: User) => {
    setEditModal({ isOpen: true, user });
    setActiveDropdown(null);
  };

  const onConfirmModalConfirm = async () => {
    if (!confirmModal.userId) return;

    if (confirmModal.type === 'suspend') {
      await suspendMutation.mutateAsync(confirmModal.userId);
    } else if (confirmModal.type === 'unsuspend') {
      await unsuspendMutation.mutateAsync(confirmModal.userId);
    } else if (confirmModal.type === 'delete') {
      await deleteMutation.mutateAsync(confirmModal.userId);
    }
    setConfirmModal({ isOpen: false, type: 'suspend' });
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getLicenseTypeName = (type: number) => {
    const types = ['Trial', 'Monthly', 'Yearly', 'Lifetime'];
    return types[type] || 'Unknown';
  };

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Kullanıcılar yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Kullanıcı Yönetimi</h1>
          <p className="mt-2 text-gray-600">
            Toplam {totalCount} kullanıcı
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <button className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500">
            <PlusIcon className="w-4 h-4 mr-2" />
            Yeni Kullanıcı
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="p-4">
          <div className="flex flex-col sm:flex-row sm:items-center space-y-4 sm:space-y-0 sm:space-x-4">
            {/* Search */}
            <div className="flex-1">
              <div className="relative">
                <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                <input
                  type="text"
                  placeholder="Kullanıcı ara..."
                  value={filters.search || ''}
                  onChange={(e) => handleSearch(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
              </div>
            </div>

            {/* Status Filter */}
            <div className="flex items-center space-x-2">
              <FunnelIcon className="w-5 h-5 text-gray-400" />
              <select
                value={filters.isActive === undefined ? 'all' : filters.isActive ? 'active' : 'inactive'}
                onChange={(e) => {
                  const value = e.target.value;
                  handleStatusFilter(value === 'all' ? undefined : value === 'active');
                }}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="all">Tüm Durumlar</option>
                <option value="active">Aktif</option>
                <option value="inactive">Pasif</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-2 text-gray-500">Kullanıcılar yükleniyor...</p>
          </div>
        ) : users.length === 0 ? (
          <div className="p-8 text-center">
            <UserIcon className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-sm font-medium text-gray-900">Kullanıcı bulunamadı</h3>
            <p className="mt-1 text-sm text-gray-500">Arama kriterlerinizi değiştirmeyi deneyin.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Kullanıcı
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Durum
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Lisans
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Kayıt Tarihi
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Son Giriş
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">İşlemler</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {users.map((user: User) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="flex items-center">
                        <div className="flex-shrink-0 h-10 w-10">
                          <div className="h-10 w-10 rounded-full bg-primary-100 flex items-center justify-center">
                            <UserIcon className="h-5 w-5 text-primary-600" />
                          </div>
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {user.fullName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {user.email}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={clsx(
                        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                        user.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-red-100 text-red-800'
                      )}>
                        {user.isActive ? (
                          <>
                            <CheckCircleIcon className="w-3 h-3 mr-1" />
                            Aktif
                          </>
                        ) : (
                          <>
                            <XCircleIcon className="w-3 h-3 mr-1" />
                            Pasif
                          </>
                        )}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      {user.activeLicense ? (
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {getLicenseTypeName(user.activeLicense.type)}
                          </div>
                          <div className="text-sm text-gray-500">
                            {user.activeLicense.expiresAt 
                              ? `${formatDate(user.activeLicense.expiresAt)} tarihinde bitiyor`
                              : 'Süresiz'
                            }
                          </div>
                        </div>
                      ) : (
                        <span className="text-sm text-gray-500">Lisans yok</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {formatDate(user.createdAt)}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Hiç giriş yapmadı'}
                    </td>
                    <td className="px-6 py-4 text-right text-sm font-medium">
                      <div className="relative">
                        <button
                          onClick={() => setActiveDropdown(activeDropdown === user.id ? null : user.id)}
                          className="text-gray-400 hover:text-gray-600"
                        >
                          <EllipsisVerticalIcon className="w-5 h-5" />
                        </button>
                        
                        {activeDropdown === user.id && (
                          <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-10 border border-gray-200">
                            <div className="py-1">
                              <button
                                onClick={() => handleEditUser(user)}
                                className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 w-full text-left"
                              >
                                <PencilIcon className="w-4 h-4 mr-2" />
                                Düzenle
                              </button>
                              
                              {user.isActive ? (
                                <button
                                  onClick={() => handleUserAction(user, 'suspend')}
                                  disabled={suspendMutation.isPending}
                                  className="flex items-center px-4 py-2 text-sm text-yellow-700 hover:bg-yellow-50 w-full text-left disabled:opacity-50"
                                >
                                  <XCircleIcon className="w-4 h-4 mr-2" />
                                  {suspendMutation.isPending ? 'Askıya alınıyor...' : 'Askıya Al'}
                                </button>
                              ) : (
                                <button
                                  onClick={() => handleUserAction(user, 'unsuspend')}
                                  disabled={unsuspendMutation.isPending}
                                  className="flex items-center px-4 py-2 text-sm text-green-700 hover:bg-green-50 w-full text-left disabled:opacity-50"
                                >
                                  <CheckCircleIcon className="w-4 h-4 mr-2" />
                                  {unsuspendMutation.isPending ? 'Aktifleştiriliyor...' : 'Aktifleştir'}
                                </button>
                              )}
                              
                              <button
                                onClick={() => handleUserAction(user, 'delete')}
                                disabled={deleteMutation.isPending}
                                className="flex items-center px-4 py-2 text-sm text-red-700 hover:bg-red-50 w-full text-left disabled:opacity-50"
                              >
                                <TrashIcon className="w-4 h-4 mr-2" />
                                {deleteMutation.isPending ? 'Siliniyor...' : 'Sil'}
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
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
      
      {/* Modals */}
      <UserEditModal
        isOpen={editModal.isOpen}
        onClose={() => setEditModal({ isOpen: false })}
        user={editModal.user}
        onSave={async (updatedUser) => {
          if (editModal.user) {
            await updateMutation.mutateAsync({ id: editModal.user.id, data: updatedUser });
          }
        }}
        isLoading={updateMutation.isPending}
      />

      <ConfirmModal
        isOpen={confirmModal.isOpen}
        onClose={() => setConfirmModal({ isOpen: false, type: 'suspend' })}
        onConfirm={onConfirmModalConfirm}
        title={confirmModal.type === 'delete' ? 'Kullanıcıyı Sil' : confirmModal.type === 'suspend' ? 'Kullanıcıyı Askıya Al' : 'Kullanıcıyı Aktifleştir'}
        message={confirmModal.type === 'delete'
          ? `Are you sure you want to delete user ${confirmModal.userName}? This action cannot be undone.`
          : confirmModal.type === 'suspend'
            ? `Are you sure you want to suspend user ${confirmModal.userName}? They will not be able to log in.`
            : `Are you sure you want to activate user ${confirmModal.userName}? They will be able to log in.`
        }
        confirmText={confirmModal.type === 'delete' ? 'Sil' : confirmModal.type === 'suspend' ? 'Askıya Al' : 'Aktifleştir'}
        type={confirmModal.type === 'delete' ? 'danger' : confirmModal.type === 'suspend' ? 'warning' : 'info'}
        isLoading={suspendMutation.isPending || unsuspendMutation.isPending || deleteMutation.isPending}
      />
    </div>
  );
}