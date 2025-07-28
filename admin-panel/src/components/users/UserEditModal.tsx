import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { XMarkIcon, UserIcon, CheckIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

interface UserEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: Partial<UserFormData>) => void;
  user?: UserFormData;
  isLoading?: boolean;
}

const userEditSchema = z.object({
  firstName: z.string().min(1, 'Ad alanı boş bırakılamaz').optional(),
  lastName: z.string().min(1, 'Soyad alanı boş bırakılamaz').optional(),
  email: z.string().email('Geçerli bir email adresi girin').optional(),
  isActive: z.boolean().optional(),
});

type UserFormData = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  fullName: string;
};

export function UserEditModal({
  isOpen,
  onClose,
  onSave,
  user,
  isLoading = false,
}: UserEditModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Partial<UserFormData>>({
    resolver: zodResolver(userEditSchema),
    defaultValues: user,
  });

  useEffect(() => {
    if (user) {
      reset(user);
    } else {
      reset({});
    }
  }, [user, reset]);

  const onSubmit = async (data: Partial<UserFormData>) => {
    onSave(data);
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
            <h3 className="text-lg font-medium text-gray-900">Kullanıcıyı Düzenle</h3>
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
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                  Ad
                </label>
                <input
                  type="text"
                  id="firstName"
                  {...register('firstName')}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                    errors.firstName ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isLoading}
                />
                {errors.firstName && (
                  <p className="mt-1 text-sm text-danger-600">{errors.firstName.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                  Soyad
                </label>
                <input
                  type="text"
                  id="lastName"
                  {...register('lastName')}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                    errors.lastName ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isLoading}
                />
                {errors.lastName && (
                  <p className="mt-1 text-sm text-danger-600">{errors.lastName.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                  Email Adresi
                </label>
                <input
                  type="email"
                  id="email"
                  {...register('email')}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                    errors.email ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isLoading}
                />
                {errors.email && (
                  <p className="mt-1 text-sm text-danger-600">{errors.email.message}</p>
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