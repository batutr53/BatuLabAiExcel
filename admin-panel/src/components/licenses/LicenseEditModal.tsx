import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { XMarkIcon, DocumentTextIcon, CheckIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

interface LicenseEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: Partial<LicenseFormData>) => void;
  license?: LicenseFormData;
  isLoading?: boolean;
}

const licenseEditSchema = z.object({
  type: z.number().optional(),
  status: z.number().optional(),
  isActive: z.boolean().optional(),
  expiresAt: z.string().optional().nullable(),
});

type LicenseFormData = {
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
};

const LICENSE_TYPES = [
  { value: 0, label: 'Trial' },
  { value: 1, label: 'Monthly' },
  { value: 2, label: 'Yearly' },
  { value: 3, label: 'Lifetime' },
];

const LICENSE_STATUS = [
  { value: 0, label: 'Pending' },
  { value: 1, label: 'Active' },
  { value: 2, label: 'Expired' },
  { value: 3, label: 'Cancelled' },
  { value: 4, label: 'Suspended' },
];

export function LicenseEditModal({
  isOpen,
  onClose,
  onSave,
  license,
  isLoading = false,
}: LicenseEditModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<Partial<LicenseFormData>>({
    resolver: zodResolver(licenseEditSchema),
    defaultValues: license,
  });

  const selectedLicenseType = watch('type');

  useEffect(() => {
    if (license) {
      reset({
        ...license,
        expiresAt: license.expiresAt ? new Date(license.expiresAt).toISOString().split('T')[0] : '',
      });
    } else {
      reset({});
    }
  }, [license, reset]);

  const onSubmit = async (data: Partial<LicenseFormData>) => {
    onSave({
      ...data,
      expiresAt: data.expiresAt === '' ? null : data.expiresAt, // Convert empty string to null for optional date
    });
  };

  const handleClose = () => {
    if (!isLoading) {
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
        <div className="relative transform overflow-hidden rounded-lg bg-white shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-md">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Lisansı Düzenle</h3>
            <button
              onClick={handleClose}
              disabled={isLoading}
              className="rounded-md text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          {/* Content */}
          <form onSubmit={(e) => void handleSubmit(onSubmit)(e)}>
            <div className="px-6 py-4 space-y-4">
              <div>
                <label htmlFor="licenseKey" className="block text-sm font-medium text-gray-700 mb-1">
                  Lisans Anahtarı
                </label>
                <input
                  type="text"
                  id="licenseKey"
                  value={license?.licenseKey || ''}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg bg-gray-100 cursor-not-allowed"
                  disabled
                />
              </div>

              <div>
                <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-1">
                  Lisans Tipi
                </label>
                <select
                  id="type"
                  {...register('type', { valueAsNumber: true })}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                    errors.type ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isLoading}
                >
                  {LICENSE_TYPES.map((type) => (
                    <option key={type.value} value={type.value}>
                      {type.label}
                    </option>
                  ))}
                </select>
                {errors.type && (
                  <p className="mt-1 text-sm text-danger-600">{errors.type.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
                  Durum
                </label>
                <select
                  id="status"
                  {...register('status', { valueAsNumber: true })}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                    errors.status ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isLoading}
                >
                  {LICENSE_STATUS.map((status) => (
                    <option key={status.value} value={status.value}>
                      {status.label}
                    </option>
                  ))}
                </select>
                {errors.status && (
                  <p className="mt-1 text-sm text-danger-600">{errors.status.message}</p>
                )}
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isActive"
                  {...register('isActive')}
                  className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                  disabled={isLoading}
                />
                <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
                  Aktif
                </label>
              </div>

              {selectedLicenseType !== 3 && (
                <div>
                  <label htmlFor="expiresAt" className="block text-sm font-medium text-gray-700 mb-1">
                    Bitiş Tarihi
                  </label>
                  <input
                    type="date"
                    id="expiresAt"
                    {...register('expiresAt')}
                    className={clsx(
                      'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                      errors.expiresAt ? 'border-danger-500' : 'border-gray-300'
                    )}
                    disabled={isLoading}
                  />
                  {errors.expiresAt && (
                    <p className="mt-1 text-sm text-danger-600">{errors.expiresAt.message}</p>
                  )}
                </div>
              )}
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
                className="px-4 py-2 text-sm font-medium text-white bg-primary-600 border border-transparent rounded-lg hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 flex items-center"
              >
                {isLoading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Kaydediliyor...
                  </>
                ) : (
                  <>
                    <CheckIcon className="w-4 h-4 mr-2" />
                    Kaydet
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}