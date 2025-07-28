import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeftIcon, DocumentPlusIcon, CheckIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { licenseAPI, userAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';
import { useQuery } from '@tanstack/react-query';

// Form validation schema
const licenseSchema = z.object({
  userId: z.string().min(1, 'Kullanıcı seçimi zorunludur'),
  type: z.string().min(1, 'Lisans tipi seçimi zorunludur'),
  expiresAt: z.string().optional(), // Optional for lifetime licenses
});

type LicenseFormData = z.infer<typeof licenseSchema>;

const LICENSE_TYPES = [
  { value: '0', label: 'Trial' },
  { value: '1', label: 'Monthly' },
  { value: '2', label: 'Yearly' },
  { value: '3', label: 'Lifetime' },
];

export function LicenseFormPage() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<LicenseFormData>({
    resolver: zodResolver(licenseSchema),
  });

  const selectedLicenseType = watch('type');

  const { data: usersData, isLoading: usersLoading } = useQuery({
    queryKey: ['users-for-license'],
    queryFn: () => userAPI.getUsers({ page: 1, pageSize: 9999 }), // Fetch all users for selection
  });

  const onSubmit = async (data: LicenseFormData) => {
    setIsSubmitting(true);
    try {
      const payload = {
        userId: data.userId,
        type: parseInt(data.type),
        expiresAt: data.type === '3' ? undefined : data.expiresAt, // Lifetime licenses don't have expiry
      };
      await licenseAPI.createLicense(payload);
      toast.success('Lisans başarıyla oluşturuldu!');
      navigate('/licenses');
    } catch (error) {
      if (error instanceof Error) {
        toast.error('Lisans oluşturulurken hata oluştu: ' + error.message);
      } else {
        toast.error('Lisans oluşturulurken bilinmeyen bir hata oluştu.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <button
            onClick={() => navigate('/licenses')}
            className="inline-flex items-center text-gray-600 hover:text-gray-900 transition-colors"
          >
            <ArrowLeftIcon className="w-5 h-5 mr-2" />
            <span className="text-lg font-medium">Lisanslar</span>
          </button>
          <h1 className="text-3xl font-bold text-gray-900 mt-2">Yeni Lisans Oluştur</h1>
          <p className="mt-1 text-gray-600">Yeni bir lisans oluşturun ve bir kullanıcıya atayın.</p>
        </div>
      </div>

      {/* License Form */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
        <form onSubmit={(e) => void handleSubmit(onSubmit)(e)} className="space-y-6">
          {/* User Selection */}
          <div>
            <label htmlFor="userId" className="block text-sm font-medium text-gray-700 mb-1">
              Kullanıcı Seç
            </label>
            {usersLoading ? (
              <div className="w-full px-4 py-2 border border-gray-300 rounded-lg bg-gray-100 text-gray-500">
                Kullanıcılar yükleniyor...
              </div>
            ) : (
              <select
                id="userId"
                {...register('userId')}
                className={clsx(
                  'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                  errors.userId ? 'border-danger-500' : 'border-gray-300'
                )}
                disabled={isSubmitting}
              >
                <option value="">-- Kullanıcı Seçin --</option>
                {usersData?.data?.data.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.fullName} ({user.email})
                  </option>
                ))}
              </select>
            )}
            {errors.userId && (
              <p className="mt-1 text-sm text-danger-600">{errors.userId.message}</p>
            )}
          </div>

          {/* License Type */}
          <div>
            <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-1">
              Lisans Tipi
            </label>
            <select
              id="type"
              {...register('type')}
              className={clsx(
                'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                errors.type ? 'border-danger-500' : 'border-gray-300'
              )}
              disabled={isSubmitting}
            >
              <option value="">-- Tip Seçin --</option>
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

          {/* Expires At (conditional) */}
          {selectedLicenseType !== '3' && selectedLicenseType !== '' && (
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
                disabled={isSubmitting}
              />
              {errors.expiresAt && (
                <p className="mt-1 text-sm text-danger-600">{errors.expiresAt.message}</p>
              )}
            </div>
          )}

          {/* Submit Button */}
          <div className="flex justify-end space-x-3">
            <button
              type="button"
              onClick={() => navigate('/licenses')}
              className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
              disabled={isSubmitting}
            >
              <XMarkIcon className="w-4 h-4 mr-2" />
              İptal
            </button>
            <button
              type="submit"
              className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  Oluşturuluyor...
                </>
              ) : (
                <>
                  <DocumentPlusIcon className="w-4 h-4 mr-2" />
                  Lisans Oluştur
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
