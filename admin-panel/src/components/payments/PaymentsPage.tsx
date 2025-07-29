import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import * as XLSX from 'xlsx';
import { 
  CreditCardIcon,
  MagnifyingGlassIcon,
  FunnelIcon,
  EllipsisVerticalIcon,
  CheckCircleIcon,
  XCircleIcon,
  ClockIcon,
  CurrencyDollarIcon,
  ArrowPathIcon,
  DocumentArrowDownIcon
} from '@heroicons/react/24/outline';
import { paymentAPI } from '../../services/api';
import type { FilterState } from '../../types';
import { clsx } from 'clsx';
import { ConfirmModal } from '../licenses/ConfirmModal';
import { PaymentDetailModal } from './PaymentDetailModal';

interface Payment {
  id: string;
  stripePaymentIntentId: string;
  amount: number;
  currency: string;
  status: number;
  licenseType: number;
  description: string;
  createdAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
  };
}

const PAYMENT_STATUS = ['Pending', 'Processing', 'Succeeded', 'Failed', 'Cancelled', 'Refunded'];
const LICENSE_TYPES = ['Trial', 'Monthly', 'Yearly', 'Lifetime'];

export function PaymentsPage() {
  const [filters, setFilters] = useState<FilterState>({
    search: '',
    page: 1,
    pageSize: 10,
  });
  
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null);
  const [confirmModal, setConfirmModal] = useState<{
    isOpen: boolean;
    paymentId?: string;
    paymentAmount?: number;
    paymentCurrency?: string;
  }>({ isOpen: false });
  const [detailModal, setDetailModal] = useState<{
    isOpen: boolean;
    payment?: Payment;
  }>({ isOpen: false });
  const [isExporting, setIsExporting] = useState(false);
  
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['payments', filters],
    queryFn: () => paymentAPI.getPayments(filters),
  });

  const refundMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => 
      paymentAPI.refundPayment(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      setActiveDropdown(null);
      toast.success('Ödeme başarıyla iade edildi!');
    },
    onError: (error) => {
      toast.error('Ödeme iade edilirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  const payments = data?.data?.data || [];
  const totalCount = data?.data?.totalCount || 0;
  const totalPages = data?.data?.totalPages || 1;

  const handleSearch = (search: string) => {
    setFilters(prev => ({ ...prev, search, page: 1 }));
  };

  const handleStatusFilter = (status?: number) => {
    setFilters(prev => ({ ...prev, status, page: 1 }));
  };

  const handlePageChange = (page: number) => {
    setFilters(prev => ({ ...prev, page }));
  };

  const handleRefund = (payment: Payment) => {
    setConfirmModal({
      isOpen: true,
      paymentId: payment.id,
      paymentAmount: payment.amount,
      paymentCurrency: payment.currency,
    });
    setActiveDropdown(null);
  };

  const handleViewDetails = (payment: Payment) => {
    setDetailModal({
      isOpen: true,
      payment: payment,
    });
    setActiveDropdown(null);
  };

  const onConfirmModalConfirm = async (reason?: string) => {
    if (confirmModal.paymentId) {
      await refundMutation.mutateAsync({ id: confirmModal.paymentId, reason });
      setConfirmModal({ isOpen: false });
    }
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

  const formatAmount = (amount: number, currency: string) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency.toUpperCase(),
    }).format(amount);
  };

  const getStatusColor = (status: number) => {
    switch (status) {
      case 0: return 'bg-yellow-100 text-yellow-800'; // Pending
      case 1: return 'bg-blue-100 text-blue-800'; // Processing
      case 2: return 'bg-green-100 text-green-800'; // Succeeded
      case 3: return 'bg-red-100 text-red-800'; // Failed
      case 4: return 'bg-gray-100 text-gray-800'; // Cancelled
      case 5: return 'bg-orange-100 text-orange-800'; // Refunded
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusIcon = (status: number) => {
    switch (status) {
      case 0: return ClockIcon; // Pending
      case 1: return ArrowPathIcon; // Processing
      case 2: return CheckCircleIcon; // Succeeded
      case 3: return XCircleIcon; // Failed
      case 4: return XCircleIcon; // Cancelled
      case 5: return ArrowPathIcon; // Refunded
      default: return ClockIcon;
    }
  };

  const getLicenseTypeColor = (type: number) => {
    switch (type) {
      case 0: return 'bg-blue-100 text-blue-800'; // Trial
      case 1: return 'bg-purple-100 text-purple-800'; // Monthly
      case 2: return 'bg-indigo-100 text-indigo-800'; // Yearly
      case 3: return 'bg-emerald-100 text-emerald-800'; // Lifetime
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const calculateTotalRevenue = () => {
    return payments
      .filter((p: Payment) => p.status === 2) // Succeeded payments only
      .reduce((sum: number, p: Payment) => sum + p.amount, 0);
  };

  const handleExportToExcel = async () => {
    try {
      setIsExporting(true);
      
      // Get all payments for export (not just current page)
      const allPaymentsResponse = await paymentAPI.getPayments({ 
        page: 1, 
        pageSize: 9999, // Get all payments
        search: filters.search,
        status: filters.status 
      });
      
      const allPayments = allPaymentsResponse?.data?.data || [];
      
      if (allPayments.length === 0) {
        toast.error('Dışa aktarılacak ödeme bulunamadı');
        return;
      }

      // Prepare data for Excel
      const exportData = allPayments.map((payment: Payment, index: number) => ({
        'Sıra No': index + 1,
        'Ödeme ID': payment.id,
        'Stripe Payment Intent ID': payment.stripePaymentIntentId,
        'Müşteri Adı': payment.user.fullName,
        'E-posta': payment.user.email,
        'Tutar': payment.amount,
        'Para Birimi': payment.currency.toUpperCase(),
        'Formatlanmış Tutar': formatAmount(payment.amount, payment.currency),
        'Lisans Tipi': LICENSE_TYPES[payment.licenseType] || 'Unknown',
        'Durum': PAYMENT_STATUS[payment.status] || 'Unknown',
        'Açıklama': payment.description,
        'Ödeme Tarihi': formatDate(payment.createdAt),
        'Ödeme Tarihi (ISO)': payment.createdAt,
        'Müşteri ID': payment.user.id,
        'İsim': payment.user.firstName,
        'Soyisim': payment.user.lastName
      }));

      // Create workbook and worksheet
      const wb = XLSX.utils.book_new();
      const ws = XLSX.utils.json_to_sheet(exportData);

      // Set column widths
      const columnWidths = [
        { wch: 8 },   // Sıra No
        { wch: 40 },  // Ödeme ID
        { wch: 35 },  // Stripe Payment Intent ID
        { wch: 25 },  // Müşteri Adı
        { wch: 30 },  // E-posta
        { wch: 12 },  // Tutar
        { wch: 8 },   // Para Birimi
        { wch: 15 },  // Formatlanmış Tutar
        { wch: 12 },  // Lisans Tipi
        { wch: 12 },  // Durum
        { wch: 40 },  // Açıklama
        { wch: 20 },  // Ödeme Tarihi
        { wch: 25 },  // Ödeme Tarihi (ISO)
        { wch: 40 },  // Müşteri ID
        { wch: 15 },  // İsim
        { wch: 15 },  // Soyisim
      ];
      ws['!cols'] = columnWidths;

      // Add summary sheet
      const summaryData = [
        { 'Özet Bilgi': 'Toplam Ödeme Sayısı', 'Değer': allPayments.length },
        { 'Özet Bilgi': 'Başarılı Ödemeler', 'Değer': allPayments.filter((p: Payment) => p.status === 2).length },
        { 'Özet Bilgi': 'Bekleyen Ödemeler', 'Değer': allPayments.filter((p: Payment) => p.status === 0).length },
        { 'Özet Bilgi': 'Başarısız Ödemeler', 'Değer': allPayments.filter((p: Payment) => p.status === 3).length },
        { 'Özet Bilgi': 'İade Edilenler', 'Değer': allPayments.filter((p: Payment) => p.status === 5).length },
        { 'Özet Bilgi': 'Toplam Gelir (USD)', 'Değer': formatAmount(calculateTotalRevenue(), 'USD') },
        { 'Özet Bilgi': 'Dışa Aktarım Tarihi', 'Değer': new Date().toLocaleString('tr-TR') },
      ];

      const summaryWs = XLSX.utils.json_to_sheet(summaryData);
      summaryWs['!cols'] = [{ wch: 25 }, { wch: 30 }];

      // Add worksheets to workbook
      XLSX.utils.book_append_sheet(wb, summaryWs, 'Özet');
      XLSX.utils.book_append_sheet(wb, ws, 'Ödemeler');

      // Create status-based sheets
      const statusSheets = [
        { status: 2, name: 'Başarılı Ödemeler', filter: (p: Payment) => p.status === 2 },
        { status: 0, name: 'Bekleyen Ödemeler', filter: (p: Payment) => p.status === 0 },
        { status: 3, name: 'Başarısız Ödemeler', filter: (p: Payment) => p.status === 3 },
        { status: 5, name: 'İade Edilenler', filter: (p: Payment) => p.status === 5 },
      ];

      statusSheets.forEach(({ name, filter }) => {
        const filteredPayments = allPayments.filter(filter);
        if (filteredPayments.length > 0) {
          const statusData = filteredPayments.map((payment: Payment, index: number) => ({
            'Sıra No': index + 1,
            'Ödeme ID': payment.id,
            'Müşteri': payment.user.fullName,
            'E-posta': payment.user.email,
            'Tutar': formatAmount(payment.amount, payment.currency),
            'Açıklama': payment.description,
            'Tarih': formatDate(payment.createdAt)
          }));
          const statusWs = XLSX.utils.json_to_sheet(statusData);
          statusWs['!cols'] = [
            { wch: 8 }, { wch: 40 }, { wch: 25 }, { wch: 30 }, 
            { wch: 15 }, { wch: 40 }, { wch: 20 }
          ];
          XLSX.utils.book_append_sheet(wb, statusWs, name);
        }
      });

      // Generate filename with timestamp
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `Odemeler_${timestamp}.xlsx`;

      // Write and download file
      XLSX.writeFile(wb, filename);
      
      toast.success(`${allPayments.length} ödeme başarıyla Excel'e aktarıldı!`);
    } catch (error) {
      console.error('Excel export error:', error);
      toast.error('Excel dışa aktarım sırasında hata oluştu');
    } finally {
      setIsExporting(false);
    }
  };

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Ödemeler yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Ödeme Yönetimi</h1>
          <p className="mt-2 text-gray-600">
            Toplam {totalCount} ödeme • Toplam gelir: {formatAmount(calculateTotalRevenue(), 'USD')}
          </p>
        </div>
        <div className="mt-4 sm:mt-0 flex space-x-3">
          <button 
            onClick={handleExportToExcel}
            disabled={isExporting}
            className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isExporting ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-700 mr-2"></div>
                Dışa Aktarılıyor...
              </>
            ) : (
              <>
                <DocumentArrowDownIcon className="w-4 h-4 mr-2" />
                Dışa Aktar
              </>
            )}
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <CurrencyDollarIcon className="h-8 w-8 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Toplam Gelir</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatAmount(calculateTotalRevenue(), 'USD')}
              </p>
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <CheckCircleIcon className="h-8 w-8 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Başarılı Ödemeler</p>
              <p className="text-2xl font-bold text-gray-900">
                {payments.filter((p: Payment) => p.status === 2).length}
              </p>
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <ClockIcon className="h-8 w-8 text-yellow-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Bekleyen Ödemeler</p>
              <p className="text-2xl font-bold text-gray-900">
                {payments.filter((p: Payment) => p.status === 0).length}
              </p>
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <XCircleIcon className="h-8 w-8 text-red-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Başarısız Ödemeler</p>
              <p className="text-2xl font-bold text-gray-900">
                {payments.filter((p: Payment) => p.status === 3).length}
              </p>
            </div>
          </div>
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
                  placeholder="Ödeme ID veya kullanıcı ara..."
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
                value={filters.status || 'all'}
                onChange={(e) => {
                  const value = e.target.value;
                  handleStatusFilter(value === 'all' ? undefined : parseInt(value));
                }}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="all">Tüm Durumlar</option>
                {PAYMENT_STATUS.map((status, index) => (
                  <option key={index} value={index}>{status}</option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Payments Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-2 text-gray-500">Ödemeler yükleniyor...</p>
          </div>
        ) : payments.length === 0 ? (
          <div className="p-8 text-center">
            <CreditCardIcon className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-sm font-medium text-gray-900">Ödeme bulunamadı</h3>
            <p className="mt-1 text-sm text-gray-500">Arama kriterlerinizi değiştirmeyi deneyin.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Ödeme
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Kullanıcı
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tutar
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Lisans Tipi
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Durum
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tarih
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">İşlemler</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {payments.map((payment: Payment) => {
                  const StatusIcon = getStatusIcon(payment.status);
                  
                  return (
                    <tr key={payment.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4">
                        <div className="flex items-center">
                          <div className="flex-shrink-0 h-10 w-10">
                            <div className="h-10 w-10 rounded-full bg-blue-100 flex items-center justify-center">
                              <CreditCardIcon className="h-5 w-5 text-blue-600" />
                            </div>
                          </div>
                          <div className="ml-4">
                            <div className="text-sm font-medium text-gray-900 font-mono">
                              {payment.stripePaymentIntentId.slice(0, 20)}...
                            </div>
                            <div className="text-sm text-gray-500">
                              {payment.description}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {payment.user.fullName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {payment.user.email}
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div className="text-sm font-bold text-gray-900">
                          {formatAmount(payment.amount, payment.currency)}
                        </div>
                        <div className="text-sm text-gray-500">
                          {payment.currency.toUpperCase()}
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className={clsx(
                          'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                          getLicenseTypeColor(payment.licenseType)
                        )}>
                          {LICENSE_TYPES[payment.licenseType] || 'Unknown'}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <span className={clsx(
                          'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                          getStatusColor(payment.status)
                        )}>
                          <StatusIcon className="w-3 h-3 mr-1" />
                          {PAYMENT_STATUS[payment.status] || 'Unknown'}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {formatDate(payment.createdAt)}
                      </td>
                      <td className="px-6 py-4 text-right text-sm font-medium">
                        <div className="relative">
                          <button
                            onClick={() => setActiveDropdown(activeDropdown === payment.id ? null : payment.id)}
                            className="text-gray-400 hover:text-gray-600"
                          >
                            <EllipsisVerticalIcon className="w-5 h-5" />
                          </button>
                          
                          {activeDropdown === payment.id && (
                            <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-10 border border-gray-200">
                              <div className="py-1">
                                <button 
                                  onClick={() => handleViewDetails(payment)}
                                  className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 w-full text-left"
                                >
                                  <DocumentArrowDownIcon className="w-4 h-4 mr-2" />
                                  Detayları Görüntüle
                                </button>
                                
                                {payment.status === 2 && ( // Succeeded payments can be refunded
                                  <button
                                    onClick={() => handleRefund(payment)}
                                    disabled={refundMutation.isPending}
                                    className="flex items-center px-4 py-2 text-sm text-orange-700 hover:bg-orange-50 w-full text-left disabled:opacity-50"
                                  >
                                    <ArrowPathIcon className="w-4 h-4 mr-2" />
                                    {refundMutation.isPending ? 'İade ediliyor...' : 'İade Et'}
                                  </button>
                                )}
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
      <PaymentDetailModal
        isOpen={detailModal.isOpen}
        onClose={() => setDetailModal({ isOpen: false })}
        payment={detailModal.payment}
      />
      
      <ConfirmModal
        isOpen={confirmModal.isOpen}
        onClose={() => setConfirmModal({ isOpen: false })}
        onConfirm={() => onConfirmModalConfirm()}
        title="Ödemeyi İade Et"
        message={`Bu ödemeyi (${formatAmount(confirmModal.paymentAmount || 0, confirmModal.paymentCurrency || 'USD')}) iade etmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`}
        confirmText="İade Et"
        type="warning"
        isLoading={refundMutation.isPending}
      />
    </div>
  );
}