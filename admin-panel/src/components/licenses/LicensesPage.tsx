import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { 
  DocumentTextIcon,
  MagnifyingGlassIcon,
  FunnelIcon,
  EllipsisVerticalIcon,
  CheckCircleIcon,
  XCircleIcon,
  PlusIcon,
  TrashIcon,
  ClockIcon,
  CalendarIcon
} from '@heroicons/react/24/outline';
import { licenseAPI } from '../../services/api';
import type { FilterState } from '../../types';
import { clsx } from 'clsx';
import { ExtendLicenseModal } from './ExtendLicenseModal';
import { ConfirmModal } from './ConfirmModal';

interface License {
  id: string;
  licenseKey: string;
  type: number;
  status: number;
  isActive: boolean;
  startDate: string;
  expiresAt?: string;
  createdAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
  };
}

const LICENSE_TYPES = ['Trial', 'Monthly', 'Yearly', 'Lifetime'];
const LICENSE_STATUS = ['Pending', 'Active', 'Expired', 'Cancelled', 'Suspended'];

export function LicensesPage() {
  const [filters, setFilters] = useState<FilterState>({
    search: '',
    page: 1,
    pageSize: 10,
  });
  
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null);
  const [extendModal, setExtendModal] = useState<{ isOpen: boolean; licenseId?: string; licenseName?: string }>({
    isOpen: false
  });
  const [confirmModal, setConfirmModal] = useState<{
    isOpen: boolean;
    type: 'revoke' | 'delete';
    licenseId?: string;
    licenseName?: string;
  }>({ isOpen: false, type: 'revoke' });
  
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['licenses', filters],
    queryFn: () => licenseAPI.getLicenses(filters),
  });

  const revokeMutation = useMutation({
    mutationFn: (licenseId: string) => licenseAPI.revokeLicense(licenseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['licenses'] });
      setActiveDropdown(null);
      toast.success('Lisans başarıyla iptal edildi');
    },
    onError: (error) => {
      toast.error('Lisans iptal edilirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const extendMutation = useMutation({
    mutationFn: ({ id, days }: { id: string; days: number }) => licenseAPI.extendLicense(id, days),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['licenses'] });
      setActiveDropdown(null);
      toast.success(`Lisans ${variables.days} gün uzatıldı`);
    },
    onError: (error) => {
      toast.error('Lisans uzatılırken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (licenseId: string) => licenseAPI.deleteLicense(licenseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['licenses'] });
      setActiveDropdown(null);
      toast.success('Lisans başarıyla silindi');
    },
    onError: (error) => {
      toast.error('Lisans silinirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const licenses = data?.data?.data || [];
  const totalCount = data?.data?.totalCount || 0;
  const totalPages = data?.data?.totalPages || 1;

  const handleSearch = (search: string) => {
    setFilters(prev => ({ ...prev, search, page: 1 }));
  };

  const handleTypeFilter = (type?: number) => {
    setFilters(prev => ({ ...prev, type, page: 1 }));
  };

  const handleStatusFilter = (isActive?: boolean) => {
    setFilters(prev => ({ ...prev, isActive, page: 1 }));
  };

  const handlePageChange = (page: number) => {
    setFilters(prev => ({ ...prev, page }));
  };

  const handleRevokeLicense = (license: License) => {
    setConfirmModal({
      isOpen: true,
      type: 'revoke',
      licenseId: license.id,
      licenseName: `${license.licenseKey} (${license.user.fullName})`
    });
    setActiveDropdown(null);
  };

  const handleExtendLicense = (license: License) => {
    setExtendModal({
      isOpen: true,
      licenseId: license.id,
      licenseName: `${license.licenseKey} (${license.user.fullName})`
    });
    setActiveDropdown(null);
  };

  const handleDeleteLicense = (license: License) => {
    setConfirmModal({
      isOpen: true,
      type: 'delete',
      licenseId: license.id,
      licenseName: `${license.licenseKey} (${license.user.fullName})`
    });
    setActiveDropdown(null);
  };

  const onExtendConfirm = async (days: number) => {
    if (extendModal.licenseId) {
      await extendMutation.mutateAsync({ id: extendModal.licenseId, days });
      setExtendModal({ isOpen: false });
    }
  };

  const onConfirmModalConfirm = async () => {
    if (!confirmModal.licenseId) return;

    if (confirmModal.type === 'revoke') {
      await revokeMutation.mutateAsync(confirmModal.licenseId);
    } else {
      await deleteMutation.mutateAsync(confirmModal.licenseId);
    }
    setConfirmModal({ isOpen: false, type: 'revoke' });
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

  const getStatusColor = (status: number, isActive: boolean) => {
    if (!isActive) return 'bg-red-100 text-red-800';
    
    switch (status) {
      case 1: return 'bg-green-100 text-green-800'; // Active
      case 0: return 'bg-yellow-100 text-yellow-800'; // Pending
      case 2: return 'bg-gray-100 text-gray-800'; // Expired
      case 3: return 'bg-red-100 text-red-800'; // Cancelled
      case 4: return 'bg-orange-100 text-orange-800'; // Suspended
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getTypeColor = (type: number) => {
    switch (type) {
      case 0: return 'bg-blue-100 text-blue-800'; // Trial
      case 1: return 'bg-purple-100 text-purple-800'; // Monthly
      case 2: return 'bg-indigo-100 text-indigo-800'; // Yearly
      case 3: return 'bg-emerald-100 text-emerald-800'; // Lifetime
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getRemainingDays = (expiresAt?: string) => {
    if (!expiresAt) return null;
    const expiry = new Date(expiresAt);
    const now = new Date();
    const diffTime = expiry.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  };

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Lisanslar yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Lisans Yönetimi</h1>
          <p className="mt-2 text-gray-600">
            Toplam {totalCount} lisans
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <button className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500">
            <PlusIcon className="w-4 h-4 mr-2" />
            Yeni Lisans
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="p-4">
          <div className="flex flex-col lg:flex-row lg:items-center space-y-4 lg:space-y-0 lg:space-x-4">
            {/* Search */}
            <div className="flex-1">
              <div className="relative">
                <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                <input
                  type="text"
                  placeholder="Lisans anahtarı veya kullanıcı ara..."
                  value={filters.search || ''}
                  onChange={(e) => handleSearch(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
              </div>
            </div>

            {/* Type Filter */}
            <div className="flex items-center space-x-2">
              <FunnelIcon className="w-5 h-5 text-gray-400" />
              <select
                value={filters.type || 'all'}
                onChange={(e) => {
                  const value = e.target.value;
                  handleTypeFilter(value === 'all' ? undefined : parseInt(value));
                }}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="all">Tüm Tipler</option>
                {LICENSE_TYPES.map((type, index) => (
                  <option key={index} value={index}>{type}</option>
                ))}
              </select>
            </div>

            {/* Status Filter */}
            <div className="flex items-center space-x-2">
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

      {/* Licenses Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-2 text-gray-500">Lisanslar yükleniyor...</p>
          </div>
        ) : licenses.length === 0 ? (
          <div className="p-8 text-center">
            <DocumentTextIcon className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-sm font-medium text-gray-900">Lisans bulunamadı</h3>
            <p className="mt-1 text-sm text-gray-500">Arama kriterlerinizi değiştirmeyi deneyin.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Lisans
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Kullanıcı
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tip
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Durum
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Bitiş Tarihi
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Oluşturulma
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">İşlemler</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {licenses.map((license: License) => {
                  const remainingDays = getRemainingDays(license.expiresAt);
                  
                  return (
                    <tr key={license.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4">
                        <div className="flex items-center">
                          <div className="flex-shrink-0 h-10 w-10">
                            <div className="h-10 w-10 rounded-full bg-indigo-100 flex items-center justify-center">
                              <DocumentTextIcon className="h-5 w-5 text-indigo-600" />
                            </div>
                          </div>
                          <div className="ml-4">
                            <div className="text-sm font-medium text-gray-900 font-mono">
                              {license.licenseKey}
                            </div>
                            <div className="text-sm text-gray-500">
                              ID: {license.id.slice(0, 8)}...
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {license.user.fullName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {license.user.email}
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className={clsx(
                          'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                          getTypeColor(license.type)
                        )}>
                          {LICENSE_TYPES[license.type] || 'Unknown'}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center space-x-2">
                          <span className={clsx(
                            'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                            getStatusColor(license.status, license.isActive)
                          )}>
                            {license.isActive ? (
                              <>
                                <CheckCircleIcon className="w-3 h-3 mr-1" />
                                {LICENSE_STATUS[license.status] || 'Unknown'}
                              </>
                            ) : (
                              <>
                                <XCircleIcon className="w-3 h-3 mr-1" />
                                İptal
                              </>
                            )}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        {license.expiresAt ? (
                          <div>
                            <div className="text-sm text-gray-900">
                              {formatDate(license.expiresAt)}
                            </div>
                            {remainingDays !== null && (
                              <div className={clsx(
                                'text-xs',
                                remainingDays > 30 ? 'text-green-600' :
                                remainingDays > 7 ? 'text-yellow-600' :
                                remainingDays > 0 ? 'text-orange-600' : 'text-red-600'
                              )}>
                                {remainingDays > 0 ? `${remainingDays} gün kaldı` : 'Süresi doldu'}
                              </div>
                            )}
                          </div>
                        ) : (
                          <div className="text-sm text-green-600 font-medium">
                            Süresiz
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        <div className="flex items-center">
                          <CalendarIcon className="w-4 h-4 mr-1" />
                          {formatDate(license.createdAt)}
                        </div>
                      </td>
                      <td className="px-6 py-4 text-right text-sm font-medium">
                        <div className="relative">
                          <button
                            onClick={() => setActiveDropdown(activeDropdown === license.id ? null : license.id)}
                            className="text-gray-400 hover:text-gray-600"
                          >
                            <EllipsisVerticalIcon className="w-5 h-5" />
                          </button>
                          
                          {activeDropdown === license.id && (
                            <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-10 border border-gray-200">
                              <div className="py-1">
                                {license.isActive && license.expiresAt && (
                                  <button
                                    onClick={() => handleExtendLicense(license)}
                                    disabled={extendMutation.isPending}
                                    className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 w-full text-left disabled:opacity-50"
                                  >
                                    <ClockIcon className="w-4 h-4 mr-2" />
                                    {extendMutation.isPending ? 'Uzatılıyor...' : 'Süre Uzat'}
                                  </button>
                                )}
                                
                                {license.isActive && (
                                  <button
                                    onClick={() => handleRevokeLicense(license)}
                                    disabled={revokeMutation.isPending}
                                    className="flex items-center px-4 py-2 text-sm text-yellow-700 hover:bg-yellow-50 w-full text-left disabled:opacity-50"
                                  >
                                    <XCircleIcon className="w-4 h-4 mr-2" />
                                    {revokeMutation.isPending ? 'İptal ediliyor...' : 'İptal Et'}
                                  </button>
                                )}
                                
                                <button
                                  onClick={() => handleDeleteLicense(license)}
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
                  );
                })}
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
      <ExtendLicenseModal
        isOpen={extendModal.isOpen}
        onClose={() => setExtendModal({ isOpen: false })}
        onConfirm={onExtendConfirm}
        isLoading={extendMutation.isPending}
        licenseName={extendModal.licenseName}
      />
      
      <ConfirmModal
        isOpen={confirmModal.isOpen}
        onClose={() => setConfirmModal({ isOpen: false, type: 'revoke' })}
        onConfirm={onConfirmModalConfirm}
        title={confirmModal.type === 'revoke' ? 'Lisansı İptal Et' : 'Lisansı Sil'}
        message={confirmModal.type === 'revoke' 
          ? `${confirmModal.licenseName} lisansını iptal etmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`
          : `${confirmModal.licenseName} lisansını silmek istediğinizden emin misiniz? Bu işlem geri alınamaz ve tüm ilgili veriler silinecektir.`
        }
        confirmText={confirmModal.type === 'revoke' ? 'İptal Et' : 'Sil'}
        type={confirmModal.type === 'revoke' ? 'warning' : 'danger'}
        isLoading={confirmModal.type === 'revoke' ? revokeMutation.isPending : deleteMutation.isPending}
      />
    </div>
  );
}