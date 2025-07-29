import { XMarkIcon, CreditCardIcon, CheckCircleIcon, XCircleIcon, ClockIcon, ArrowPathIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

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

interface PaymentDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  payment?: Payment;
}

const PAYMENT_STATUS = ['Pending', 'Processing', 'Succeeded', 'Failed', 'Cancelled', 'Refunded'];
const LICENSE_TYPES = ['Trial', 'Monthly', 'Yearly', 'Lifetime'];

export function PaymentDetailModal({ isOpen, onClose, payment }: PaymentDetailModalProps) {
  if (!isOpen || !payment) return null;

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
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
      case 0: return 'bg-yellow-100 text-yellow-800 border-yellow-200'; // Pending
      case 1: return 'bg-blue-100 text-blue-800 border-blue-200'; // Processing
      case 2: return 'bg-green-100 text-green-800 border-green-200'; // Succeeded
      case 3: return 'bg-red-100 text-red-800 border-red-200'; // Failed
      case 4: return 'bg-gray-100 text-gray-800 border-gray-200'; // Cancelled
      case 5: return 'bg-orange-100 text-orange-800 border-orange-200'; // Refunded
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
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
      case 0: return 'bg-blue-100 text-blue-800 border-blue-200'; // Trial
      case 1: return 'bg-purple-100 text-purple-800 border-purple-200'; // Monthly
      case 2: return 'bg-indigo-100 text-indigo-800 border-indigo-200'; // Yearly
      case 3: return 'bg-emerald-100 text-emerald-800 border-emerald-200'; // Lifetime
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const StatusIcon = getStatusIcon(payment.status);

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div className="relative transform overflow-hidden rounded-lg bg-white shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-2xl">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-gray-50">
            <div className="flex items-center space-x-3">
              <div className="flex-shrink-0 h-10 w-10">
                <div className="h-10 w-10 rounded-full bg-blue-100 flex items-center justify-center">
                  <CreditCardIcon className="h-6 w-6 text-blue-600" />
                </div>
              </div>
              <div>
                <h3 className="text-lg font-medium text-gray-900">Ödeme Detayları</h3>
                <p className="text-sm text-gray-500">#{payment.id.slice(0, 8)}...</p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="rounded-md text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          {/* Content */}
          <div className="px-6 py-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Payment Information */}
              <div className="space-y-4">
                <h4 className="text-lg font-medium text-gray-900 mb-4">Ödeme Bilgileri</h4>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Stripe Payment Intent ID
                  </label>
                  <div className="px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg font-mono text-sm">
                    {payment.stripePaymentIntentId}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Tutar
                  </label>
                  <div className="text-2xl font-bold text-gray-900">
                    {formatAmount(payment.amount, payment.currency)}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Durum
                  </label>
                  <span className={clsx(
                    'inline-flex items-center px-3 py-1 rounded-full text-sm font-medium border',
                    getStatusColor(payment.status)
                  )}>
                    <StatusIcon className="w-4 h-4 mr-2" />
                    {PAYMENT_STATUS[payment.status] || 'Unknown'}
                  </span>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Lisans Tipi
                  </label>
                  <span className={clsx(
                    'inline-flex items-center px-3 py-1 rounded-full text-sm font-medium border',
                    getLicenseTypeColor(payment.licenseType)
                  )}>
                    {LICENSE_TYPES[payment.licenseType] || 'Unknown'}
                  </span>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Açıklama
                  </label>
                  <div className="px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg text-sm">
                    {payment.description}
                  </div>
                </div>
              </div>

              {/* Customer Information */}
              <div className="space-y-4">
                <h4 className="text-lg font-medium text-gray-900 mb-4">Müşteri Bilgileri</h4>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Müşteri ID
                  </label>
                  <div className="px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg font-mono text-sm">
                    {payment.user.id}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ad Soyad
                  </label>
                  <div className="text-lg font-medium text-gray-900">
                    {payment.user.fullName}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    E-posta
                  </label>
                  <div className="text-sm text-gray-900">
                    {payment.user.email}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    İsim
                  </label>
                  <div className="text-sm text-gray-900">
                    {payment.user.firstName} {payment.user.lastName}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ödeme Tarihi
                  </label>
                  <div className="text-sm text-gray-900">
                    {formatDate(payment.createdAt)}
                  </div>
                </div>
              </div>
            </div>

            {/* Payment Timeline */}
            <div className="mt-8 pt-6 border-t border-gray-200">
              <h4 className="text-lg font-medium text-gray-900 mb-4">Ödeme Geçmişi</h4>
              <div className="flow-root">
                <ul className="-mb-8">
                  <li>
                    <div className="relative pb-8">
                      <div className="relative flex space-x-3">
                        <div>
                          <span className={clsx(
                            'h-8 w-8 rounded-full flex items-center justify-center ring-8 ring-white',
                            payment.status === 2 ? 'bg-green-500' :
                            payment.status === 3 ? 'bg-red-500' :
                            payment.status === 0 ? 'bg-yellow-500' : 'bg-gray-500'
                          )}>
                            <StatusIcon className="h-4 w-4 text-white" />
                          </span>
                        </div>
                        <div className="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                          <div>
                            <p className="text-sm text-gray-500">
                              Ödeme durumu: <span className="font-medium text-gray-900">{PAYMENT_STATUS[payment.status]}</span>
                            </p>
                          </div>
                          <div className="text-right text-sm whitespace-nowrap text-gray-500">
                            {formatDate(payment.createdAt)}
                          </div>
                        </div>
                      </div>
                    </div>
                  </li>
                </ul>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 flex justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Kapat
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}