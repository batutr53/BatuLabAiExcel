import { useState } from 'react';
import { XMarkIcon, ClockIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

interface ExtendLicenseModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (days: number) => void;
  isLoading?: boolean;
  licenseName?: string;
}

export function ExtendLicenseModal({ 
  isOpen, 
  onClose, 
  onConfirm, 
  isLoading = false,
  licenseName 
}: ExtendLicenseModalProps) {
  const [days, setDays] = useState<string>('30');
  const [error, setError] = useState<string>('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const daysNumber = parseInt(days);
    if (isNaN(daysNumber) || daysNumber < 1 || daysNumber > 365) {
      setError('Lütfen 1 ile 365 arasında geçerli bir gün sayısı girin.');
      return;
    }
    
    setError('');
    onConfirm(daysNumber);
  };

  const handleClose = () => {
    if (!isLoading) {
      setDays('30');
      setError('');
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
        onClick={handleClose}
      />
      
      {/* Modal */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div className="relative transform overflow-hidden rounded-lg bg-white shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <ClockIcon className="h-6 w-6 text-blue-600" />
              </div>
              <div className="ml-3">
                <h3 className="text-lg font-medium text-gray-900">
                  Lisans Süresini Uzat
                </h3>
                {licenseName && (
                  <p className="text-sm text-gray-500">
                    {licenseName}
                  </p>
                )}
              </div>
            </div>
            <button
              onClick={handleClose}
              disabled={isLoading}
              className="rounded-md text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          {/* Content */}
          <form onSubmit={handleSubmit}>
            <div className="px-6 py-4">
              <div className="space-y-4">
                <div>
                  <label htmlFor="days" className="block text-sm font-medium text-gray-700 mb-2">
                    Kaç gün uzatmak istiyorsunuz?
                  </label>
                  <div className="relative">
                    <input
                      type="number"
                      id="days"
                      value={days}
                      onChange={(e) => setDays(e.target.value)}
                      min="1"
                      max="365"
                      disabled={isLoading}
                      className={clsx(
                        "w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed",
                        error ? "border-red-300" : "border-gray-300"
                      )}
                      placeholder="30"
                    />
                    <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
                      <span className="text-gray-500 text-sm">gün</span>
                    </div>
                  </div>
                  {error && (
                    <p className="mt-1 text-sm text-red-600">{error}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    1 ile 365 gün arasında bir değer giriniz.
                  </p>
                </div>

                {/* Quick options */}
                <div>
                  <p className="text-sm font-medium text-gray-700 mb-2">Hızlı seçenekler:</p>
                  <div className="flex flex-wrap gap-2">
                    {[7, 15, 30, 60, 90, 180, 365].map((option) => (
                      <button
                        key={option}
                        type="button"
                        onClick={() => setDays(option.toString())}
                        disabled={isLoading}
                        className="px-3 py-1 text-xs border border-gray-300 rounded-full hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
                      >
                        {option} gün
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 flex justify-end space-x-3">
              <button
                type="button"
                onClick={handleClose}
                disabled={isLoading}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
              >
                İptal
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 flex items-center"
              >
                {isLoading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Uzatılıyor...
                  </>
                ) : (
                  'Uzat'
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}