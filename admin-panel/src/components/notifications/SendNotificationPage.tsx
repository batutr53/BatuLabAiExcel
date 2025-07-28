import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeftIcon, PaperAirplaneIcon, CheckIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { notificationAPI, userAPI } from '../../services/api';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';
import { useQuery } from '@tanstack/react-query';

// Form validation schema
const sendNotificationSchema = z.object({
  userIds: z.array(z.string()).optional(), // Optional for broadcast
  message: z.string().min(1, 'Mesaj alanı boş bırakılamaz'),
  type: z.enum(['info', 'success', 'warning', 'error'], { required_error: "Bildirim tipi seçimi zorunludur" }),
  isBroadcast: z.boolean().default(false),
});

type SendNotificationFormData = z.infer<typeof sendNotificationSchema>;

export function SendNotificationPage() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<SendNotificationFormData>({
    resolver: zodResolver(sendNotificationSchema),
    defaultValues: { isBroadcast: false, userIds: [] },
  });

  const isBroadcast = watch('isBroadcast');

  const { data: usersData, isLoading: usersLoading } = useQuery({
    queryKey: ['users-for-notification'],
    queryFn: () => userAPI.getUsers({ page: 1, pageSize: 9999 }), // Fetch all users for selection
    enabled: !isBroadcast,
  });

  const onSubmit = async (data: SendNotificationFormData) => {
    setIsSubmitting(true);
    try {
      if (data.isBroadcast) {
        await notificationAPI.broadcastNotification(data.message, data.type);
        toast.success('Bildirim tüm kullanıcılara başarıyla gönderildi!');
      } else {
        if (!data.userIds || data.userIds.length === 0) {
          toast.error('Lütfen bildirim göndermek için en az bir kullanıcı seçin.');
          return;
        }
        await notificationAPI.sendNotification(data.userIds, data.message, data.type);
        toast.success('Bildirim seçilen kullanıcılara başarıyla gönderildi!');
      }
      navigate('/notifications');
    } catch (error) {
      if (error instanceof Error) {
        toast.error('Bildirim gönderilirken hata oluştu: ' + error.message);
      } else {
        toast.error('Bildirim gönderilirken bilinmeyen bir hata oluştu.');
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
            onClick={() => navigate('/notifications')}
            className="inline-flex items-center text-gray-600 hover:text-gray-900 transition-colors"
          >
            <ArrowLeftIcon className="w-5 h-5 mr-2" />
            <span className="text-lg font-medium">Bildirimler</span>
          </button>
          <h1 className="text-3xl font-bold text-gray-900 mt-2">Bildirim Gönder</h1>
          <p className="mt-1 text-gray-600">Kullanıcılara özel veya genel bildirimler gönderin.</p>
        </div>
      </div>

      {/* Notification Form */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
        <form onSubmit={(e) => void handleSubmit(onSubmit)(e)} className="space-y-6">
          {/* Broadcast Toggle */}
          <div className="flex items-center">
            <input
              type="checkbox"
              id="isBroadcast"
              {...register('isBroadcast')}
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              disabled={isSubmitting}
            />
            <label htmlFor="isBroadcast" className="ml-2 block text-sm text-gray-900">
              Tüm kullanıcılara gönder (Broadcast)
            </label>
          </div>

          {/* User Selection (conditional) */}
          {!isBroadcast && (
            <div>
              <label htmlFor="userIds" className="block text-sm font-medium text-gray-700 mb-1">
                Kullanıcı Seç (Birden fazla seçilebilir)
              </label>
              {usersLoading ? (
                <div className="w-full px-4 py-2 border border-gray-300 rounded-lg bg-gray-100 text-gray-500">
                  Kullanıcılar yükleniyor...
                </div>
              ) : (
                <select
                  id="userIds"
                  multiple
                  {...register('userIds')}
                  className={clsx(
                    'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500 h-48',
                    errors.userIds ? 'border-danger-500' : 'border-gray-300'
                  )}
                  disabled={isSubmitting}
                >
                  {usersData?.data?.data.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.fullName} ({user.email})
                    </option>
                  ))}
                </select>
              )}
              {errors.userIds && (
                <p className="mt-1 text-sm text-danger-600">{errors.userIds.message}</p>
              )}
            </div>
          )}

          {/* Message */}
          <div>
            <label htmlFor="message" className="block text-sm font-medium text-gray-700 mb-1">
              Mesaj
            </label>
            <textarea
              id="message"
              rows={4}
              {...register('message')}
              className={clsx(
                'w-full px-4 py-2 border rounded-lg focus:ring-primary-500 focus:border-primary-500',
                errors.message ? 'border-danger-500' : 'border-gray-300'
              )}
              disabled={isSubmitting}
            ></textarea>
            {errors.message && (
              <p className="mt-1 text-sm text-danger-600">{errors.message.message}</p>
            )}
          </div>

          {/* Notification Type */}
          <div>
            <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-1">
              Bildirim Tipi
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
              <option value="info">Bilgi</option>
              <option value="success">Başarılı</option>
              <option value="warning">Uyarı</option>
              <option value="error">Hata</option>
            </select>
            {errors.type && (
              <p className="mt-1 text-sm text-danger-600">{errors.type.message}</p>
            )}
          </div>

          {/* Submit Button */}
          <div className="flex justify-end space-x-3">
            <button
              type="button"
              onClick={() => navigate('/notifications')}
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
                  Gönderiliyor...
                </>
              ) : (
                <>
                  <PaperAirplaneIcon className="w-4 h-4 mr-2" />
                  Bildirim Gönder
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
