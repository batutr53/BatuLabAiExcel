import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeftIcon, UserPlusIcon, CheckIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { userAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';

// Form validation schema
const userSchema = z.object({
  firstName: z.string().min(1, 'Ad alanı boş bırakılamaz'),
  lastName: z.string().min(1, 'Soyad alanı boş bırakılamaz'),
  email: z.string().email('Geçerli bir email adresi girin'),
  password: z.string().min(6, 'Şifre en az 6 karakter olmalı'),
});

type UserFormData = z.infer<typeof userSchema>;

export function UserFormPage() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
  });

  const onSubmit = async (data: UserFormData) => {
    setIsSubmitting(true);
    try {
      // Assuming userAPI.createUser exists and takes firstName, lastName, email, password
      await userAPI.createUser({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
      });
      toast.success('Kullanıcı başarıyla oluşturuldu!');
      navigate('/users');
    } catch (error) {
      if (error instanceof Error) {
        toast.error('Kullanıcı oluşturulurken hata oluştu: ' + error.message);
      } else {
        toast.error('Kullanıcı oluşturulurken bilinmeyen bir hata oluştu.');
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
            onClick={() => navigate('/users')}
            className="inline-flex items-center text-gray-600 hover:text-gray-900 transition-colors"
          >
            <ArrowLeftIcon className="w-5 h-5 mr-2" />
            <span className="text-lg font-medium">Kullanıcılar</span>
          </button>
          <h1 className="text-3xl font-bold text-gray-900 mt-2">Yeni Kullanıcı Oluştur</h1>
          <p className="mt-1 text-gray-600">Yeni bir kullanıcı hesabı oluşturun ve lisans atayın.</p>
        </div>
      </div>

      {/* User Form */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
        <form onSubmit={(e) => void handleSubmit(onSubmit)(e)} className="space-y-6">
          {/* First Name */}
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
              disabled={isSubmitting}
            />
            {errors.firstName && (
              <p className="mt-1 text-sm text-danger-600">{errors.firstName.message}</p>
            )}
          </div>

          {/* Last Name */}
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
              disabled={isSubmitting}
            />
            {errors.lastName && (
              <p className="mt-1 text-sm text-danger-600">{errors.lastName.message}</p>
            )}
          </div>

          {/* Email */}
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
              disabled={isSubmitting}
            />
            {errors.email && (
              <p className="mt-1 text-sm text-danger-600">{errors.email.message}</p>
            )}
          </div>

          {/* Password */}
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Şifre
            </label>
            <input
              type="password"
              id="password"
              {...register('password')}
              className={clsx(
                'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                errors.password ? 'border-danger-500' : 'border-gray-300'
              )}
              disabled={isSubmitting}
            />
            {errors.password && (
              <p className="mt-1 text-sm text-danger-600">{errors.password.message}</p>
            )}
          </div>

          {/* Submit Button */}
          <div className="flex justify-end space-x-3">
            <button
              type="button"
              onClick={() => navigate('/users')}
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
                  <UserPlusIcon className="w-4 h-4 mr-2" />
                  Kullanıcı Oluştur
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
