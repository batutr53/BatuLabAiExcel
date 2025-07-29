import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { settingsAPI } from '../../services/api';
import type { AdminSettings } from '../../types';
import { 
  CogIcon,
  UserIcon,
  LockClosedIcon,
  BellIcon,
  GlobeAltIcon,
  ShieldCheckIcon,
  CircleStackIcon,
  CurrencyDollarIcon,
  EnvelopeIcon,
  KeyIcon,
  CheckIcon
} from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

interface SettingSection {
  id: string;
  name: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
}

const settingSections: SettingSection[] = [
  {
    id: 'general',
    name: 'Genel Ayarlar',
    icon: CogIcon,
    description: 'Temel sistem ayarları ve yapılandırma'
  },
  {
    id: 'users',
    name: 'Kullanıcı Ayarları',
    icon: UserIcon,
    description: 'Kullanıcı kayıt ve yetkilendirme ayarları'
  },
  {
    id: 'security',
    name: 'Güvenlik',
    icon: ShieldCheckIcon,
    description: 'Güvenlik politikaları ve erişim kontrolü'
  },
  {
    id: 'notifications',
    name: 'Bildirimler',
    icon: BellIcon,
    description: 'E-posta ve sistem bildirimleri'
  },
  {
    id: 'payment',
    name: 'Ödeme Ayarları',
    icon: CurrencyDollarIcon,
    description: 'Stripe ve ödeme yapılandırması'
  },
  {
    id: 'api',
    name: 'API Ayarları',
    icon: KeyIcon,
    description: 'API anahtarları ve entegrasyon ayarları'
  }
];

export function SettingsPage() {
  const [activeSection, setActiveSection] = useState('general');
  const [settings, setSettings] = useState<AdminSettings | null>(null);
  const queryClient = useQueryClient();

  const { data: settingsData, isLoading } = useQuery({
    queryKey: ['admin-settings'],
    queryFn: () => {
      console.log('Fetching settings from API...');
      return settingsAPI.getAdminSettings();
    },
    staleTime: 0, // Always fetch fresh data
    gcTime: 0, // Don't cache
  });

  const updateSettingsMutation = useMutation({
    mutationFn: (updatedSettings: AdminSettings) => {
      console.log('Sending settings to API:', updatedSettings);
      return settingsAPI.updateAdminSettings(updatedSettings);
    },
    onSuccess: async (response) => {
      console.log('Settings update successful:', response);
      
      // Update local state immediately with response data
      if (response?.data) {
        console.log('Updating local state with:', response.data);
        setSettings(response.data);
      }
      
      // Force refetch fresh data
      await queryClient.invalidateQueries({ queryKey: ['admin-settings'] });
      await queryClient.refetchQueries({ queryKey: ['admin-settings'] });
      toast.success('Ayarlar başarıyla güncellendi!');
    },
    onError: (error: any) => {
      console.error('Settings update error:', error);
      toast.error('Ayarlar güncellenirken hata oluştu: ' + (error.message || 'Bilinmeyen hata'));
    }
  });

  useEffect(() => {
    if (settingsData?.data) {
      console.log('useEffect: Updating settings from API response:', settingsData.data);
      setSettings(settingsData.data);
    }
  }, [settingsData]);

  const handleSave = async () => {
    if (!settings) return;
    console.log('Saving settings:', settings);
    updateSettingsMutation.mutate(settings);
  };

  const updateSetting = (section: keyof AdminSettings, key: string, value: any) => {
    if (!settings) return;
    setSettings({
      ...settings,
      [section]: {
        ...settings[section],
        [key]: value
      }
    });
  };

  if (isLoading || !settings) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        <span className="ml-2 text-gray-600">Ayarlar yükleniyor...</span>
      </div>
    );
  }

  const renderGeneralSettings = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Sistem Bilgileri</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Uygulama Adı
            </label>
            <input
              type="text"
              value={settings?.general.appName || ''}
              onChange={(e) => updateSetting('general', 'appName', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Sürüm
            </label>
            <input
              type="text"
              value={settings?.general.version || ''}
              onChange={(e) => updateSetting('general', 'version', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Zaman Dilimi
            </label>
            <select 
              value={settings?.general.timeZone || 'Europe/Istanbul'}
              onChange={(e) => updateSetting('general', 'timeZone', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="Europe/Istanbul">Europe/Istanbul (GMT+3)</option>
              <option value="UTC">UTC (GMT+0)</option>
              <option value="America/New_York">America/New_York (GMT-5)</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Dil
            </label>
            <select 
              value={settings?.general.language || 'tr'}
              onChange={(e) => updateSetting('general', 'language', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="tr">Türkçe</option>
              <option value="en">English</option>
            </select>
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Maintenance Mode</h3>
        <div className="flex items-center space-x-3">
          <input
            type="checkbox"
            id="maintenance"
            checked={settings?.general.maintenanceMode || false}
            onChange={(e) => updateSetting('general', 'maintenanceMode', e.target.checked)}
            className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
          />
          <label htmlFor="maintenance" className="text-sm text-gray-700">
            Bakım modunu etkinleştir (tüm kullanıcılar dışında admin)
          </label>
        </div>
      </div>
    </div>
  );

  const renderUserSettings = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Kayıt Ayarları</h3>
        <div className="space-y-4">
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="allowRegistration"
              checked={settings?.users.allowRegistration || false}
              onChange={(e) => updateSetting('users', 'allowRegistration', e.target.checked)}
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
            />
            <label htmlFor="allowRegistration" className="text-sm text-gray-700">
              Yeni kullanıcı kaydına izin ver
            </label>
          </div>
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="emailVerification"
              checked={settings?.users.emailVerificationRequired || false}
              onChange={(e) => updateSetting('users', 'emailVerificationRequired', e.target.checked)}
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
            />
            <label htmlFor="emailVerification" className="text-sm text-gray-700">
              E-posta doğrulama zorunlu
            </label>
          </div>
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="autoTrialLicense"
              checked={settings?.users.autoTrialLicense || false}
              onChange={(e) => updateSetting('users', 'autoTrialLicense', e.target.checked)}
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
            />
            <label htmlFor="autoTrialLicense" className="text-sm text-gray-700">
              Kayıt sırasında otomatik deneme lisansı ver
            </label>
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Şifre Politikası</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Minimum Şifre Uzunluğu
            </label>
            <input
              type="number"
              value={settings?.users.minimumPasswordLength || 6}
              onChange={(e) => updateSetting('users', 'minimumPasswordLength', parseInt(e.target.value))}
              min="4"
              max="20"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Maksimum Giriş Denemesi
            </label>
            <input
              type="number"
              value={settings?.users.maxLoginAttempts || 5}
              onChange={(e) => updateSetting('users', 'maxLoginAttempts', parseInt(e.target.value))}
              min="3"
              max="10"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
        </div>
      </div>
    </div>
  );

  const renderSecuritySettings = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">JWT Ayarları</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Token Geçerlilik Süresi (saat)
            </label>
            <input
              type="number"
              value={settings?.security.tokenExpiryHours || 24}
              onChange={(e) => updateSetting('security', 'tokenExpiryHours', parseInt(e.target.value))}
              min="1"
              max="168"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Refresh Token Süresi (gün)
            </label>
            <input
              type="number"
              value={settings?.security.refreshTokenDurationDays || 30}
              onChange={(e) => updateSetting('security', 'refreshTokenDurationDays', parseInt(e.target.value))}
              min="1"
              max="90"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Rate Limiting</h3>
        <div className="space-y-4">
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="enableRateLimit"
              checked={settings?.security.enableRateLimiting || false}
              onChange={(e) => updateSetting('security', 'enableRateLimiting', e.target.checked)}
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
            />
            <label htmlFor="enableRateLimit" className="text-sm text-gray-700">
              Rate limiting'i etkinleştir
            </label>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Genel Limit (per dakika)
              </label>
              <input
                type="number"
                value={settings?.security.generalRateLimit || 100}
                onChange={(e) => updateSetting('security', 'generalRateLimit', parseInt(e.target.value))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Auth Limit (per dakika)
              </label>
              <input
                type="number"
                value={settings?.security.authRateLimit || 10}
                onChange={(e) => updateSetting('security', 'authRateLimit', parseInt(e.target.value))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Payment Limit (per dakika)
              </label>
              <input
                type="number"
                value={settings?.security.paymentRateLimit || 5}
                onChange={(e) => updateSetting('security', 'paymentRateLimit', parseInt(e.target.value))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  const renderNotificationSettings = () => {
    const notificationTypes = [
      { key: 'NewUser', label: 'Yeni kullanıcı kaydı' },
      { key: 'PaymentSuccess', label: 'Ödeme tamamlandı' },
      { key: 'PaymentFailed', label: 'Ödeme başarısız' },
      { key: 'LicenseExpired', label: 'Lisans süresi bitti' },
      { key: 'SystemError', label: 'Sistem hataları' },
      { key: 'SecurityAlert', label: 'Güvenlik uyarıları' }
    ];

    return (
      <div className="space-y-6">
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">E-posta Ayarları</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                SMTP Host
              </label>
              <input
                type="text"
                value={settings?.notifications.smtpHost || ''}
                onChange={(e) => updateSetting('notifications', 'smtpHost', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                SMTP Port
              </label>
              <input
                type="number"
                value={settings?.notifications.smtpPort || 587}
                onChange={(e) => updateSetting('notifications', 'smtpPort', parseInt(e.target.value))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Gönderen E-posta
              </label>
              <input
                type="email"
                value={settings?.notifications.fromEmail || ''}
                onChange={(e) => updateSetting('notifications', 'fromEmail', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Gönderen Adı
              </label>
              <input
                type="text"
                value={settings?.notifications.fromName || ''}
                onChange={(e) => updateSetting('notifications', 'fromName', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                SMTP Kullanıcı Adı
              </label>
              <input
                type="text"
                value={settings?.notifications.smtpUsername || ''}
                onChange={(e) => updateSetting('notifications', 'smtpUsername', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                SMTP Şifresi
              </label>
              <input
                type="password"
                value={settings?.notifications.smtpPassword || ''}
                onChange={(e) => updateSetting('notifications', 'smtpPassword', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
          <div className="mt-4">
            <div className="flex items-center space-x-3">
              <input
                type="checkbox"
                id="enableSsl"
                checked={settings?.notifications.enableSsl || false}
                onChange={(e) => updateSetting('notifications', 'enableSsl', e.target.checked)}
                className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              />
              <label htmlFor="enableSsl" className="text-sm text-gray-700">
                SSL/TLS etkinleştir
              </label>
            </div>
          </div>
        </div>

        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">Bildirim Türleri</h3>
          <div className="space-y-3">
            {notificationTypes.map((notificationType) => {
              const isEnabled = settings?.notifications.enabledNotificationTypes?.includes(notificationType.key) || false;
              return (
                <div key={notificationType.key} className="flex items-center space-x-3">
                  <input
                    type="checkbox"
                    id={notificationType.key}
                    checked={isEnabled}
                    onChange={(e) => {
                      const currentTypes = settings?.notifications.enabledNotificationTypes || [];
                      let newTypes;
                      if (e.target.checked) {
                        newTypes = [...currentTypes, notificationType.key];
                      } else {
                        newTypes = currentTypes.filter(type => type !== notificationType.key);
                      }
                      updateSetting('notifications', 'enabledNotificationTypes', newTypes);
                    }}
                    className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                  />
                  <label htmlFor={notificationType.key} className="text-sm text-gray-700">
                    {notificationType.label}
                  </label>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  };

  const renderPaymentSettings = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Stripe Yapılandırması</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Publishable Key
            </label>
            <input
              type="text"
              value={settings?.payment.stripePublishableKey || ''}
              onChange={(e) => updateSetting('payment', 'stripePublishableKey', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Secret Key
            </label>
            <input
              type="password"
              value={settings?.payment.stripeSecretKey || ''}
              onChange={(e) => updateSetting('payment', 'stripeSecretKey', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Webhook Secret
            </label>
            <input
              type="password"
              value={settings?.payment.stripeWebhookSecret || ''}
              onChange={(e) => updateSetting('payment', 'stripeWebhookSecret', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Fiyatlandırma</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Aylık Plan (USD)
            </label>
            <input
              type="number"
              value={settings?.payment.monthlyPlanPrice || 0}
              onChange={(e) => updateSetting('payment', 'monthlyPlanPrice', parseFloat(e.target.value))}
              step="0.01"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Yıllık Plan (USD)
            </label>
            <input
              type="number"
              value={settings?.payment.yearlyPlanPrice || 0}
              onChange={(e) => updateSetting('payment', 'yearlyPlanPrice', parseFloat(e.target.value))}
              step="0.01"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Yaşam Boyu Plan (USD)
            </label>
            <input
              type="number"
              value={settings?.payment.lifetimePlanPrice || 0}
              onChange={(e) => updateSetting('payment', 'lifetimePlanPrice', parseFloat(e.target.value))}
              step="0.01"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Deneme Süresi</h3>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Deneme Süresi (gün)
          </label>
          <input
            type="number"
            value={settings?.payment.trialDurationDays || 1}
            onChange={(e) => updateSetting('payment', 'trialDurationDays', parseInt(e.target.value))}
            min="1"
            max="30"
            className="w-32 border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>
      </div>
    </div>
  );

  const renderAPISettings = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Claude API</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              API Key
            </label>
            <input
              type="password"
              value={settings?.api.claudeApiKey || ''}
              onChange={(e) => updateSetting('api', 'claudeApiKey', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Model
            </label>
            <select 
              value={settings?.api.claudeModel || 'claude-3-sonnet'}
              onChange={(e) => updateSetting('api', 'claudeModel', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="claude-3-sonnet">Claude 3 Sonnet</option>
              <option value="claude-3-haiku">Claude 3 Haiku</option>
              <option value="claude-3-opus">Claude 3 Opus</option>
            </select>
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Gemini API</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              API Key
            </label>
            <input
              type="password"
              value={settings?.api.geminiApiKey || ''}
              onChange={(e) => updateSetting('api', 'geminiApiKey', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Groq API</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              API Key
            </label>
            <input
              type="password"
              value={settings?.api.groqApiKey || ''}
              onChange={(e) => updateSetting('api', 'groqApiKey', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
        </div>
      </div>
    </div>
  );

  const renderContent = () => {
    switch (activeSection) {
      case 'general': return renderGeneralSettings();
      case 'users': return renderUserSettings();
      case 'security': return renderSecuritySettings();
      case 'notifications': return renderNotificationSettings();
      case 'payment': return renderPaymentSettings();
      case 'api': return renderAPISettings();
      default: return renderGeneralSettings();
    }
  };

  return (
    <div className="flex h-full">
      {/* Sidebar */}
      <div className="w-64 bg-white border-r border-gray-200 flex-shrink-0">
        <div className="p-4">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Ayarlar</h2>
          <nav className="space-y-1">
            {settingSections.map((section) => {
              const Icon = section.icon;
              return (
                <button
                  key={section.id}
                  onClick={() => setActiveSection(section.id)}
                  className={clsx(
                    'w-full flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors',
                    activeSection === section.id
                      ? 'bg-primary-50 text-primary-700 border-r-2 border-primary-500'
                      : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                  )}
                >
                  <Icon className="w-5 h-5 mr-3" />
                  {section.name}
                </button>
              );
            })}
          </nav>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 overflow-y-auto">
        <div className="p-6">
          <div className="max-w-4xl">
            {/* Header */}
            <div className="mb-6">
              <h1 className="text-2xl font-bold text-gray-900">
                {settingSections.find(s => s.id === activeSection)?.name}
              </h1>
              <p className="mt-1 text-gray-600">
                {settingSections.find(s => s.id === activeSection)?.description}
              </p>
            </div>

            {/* Content */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              {renderContent()}

              {/* Save Button */}
              <div className="mt-8 pt-6 border-t border-gray-200">
                <div className="flex items-center justify-end space-x-3">
                  <button
                    onClick={handleSave}
                    disabled={updateSettingsMutation.isPending}
                    className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {updateSettingsMutation.isPending ? (
                      <>
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                        Kaydediliyor...
                      </>
                    ) : (
                      'Değişiklikleri Kaydet'
                    )}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}