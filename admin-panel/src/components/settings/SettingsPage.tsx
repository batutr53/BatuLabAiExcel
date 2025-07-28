import { useState } from 'react';
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
  const [isSaving, setIsSaving] = useState(false);
  const [savedSection, setSavedSection] = useState<string | null>(null);

  const handleSave = async (sectionId: string) => {
    setIsSaving(true);
    
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    setIsSaving(false);
    setSavedSection(sectionId);
    
    // Clear saved indicator after 3 seconds
    setTimeout(() => setSavedSection(null), 3000);
  };

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
              defaultValue="Office AI - Batu Lab"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Sürüm
            </label>
            <input
              type="text"
              defaultValue="1.0.0"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Zaman Dilimi
            </label>
            <select className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option value="Europe/Istanbul">Europe/Istanbul (GMT+3)</option>
              <option value="UTC">UTC (GMT+0)</option>
              <option value="America/New_York">America/New_York (GMT-5)</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Dil
            </label>
            <select className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
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
              defaultChecked
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
              defaultChecked
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
              defaultValue="6"
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
              defaultValue="5"
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
              defaultValue="24"
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
              defaultValue="30"
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
              defaultChecked
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
                defaultValue="100"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Auth Limit (per dakika)
              </label>
              <input
                type="number"
                defaultValue="10"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Payment Limit (per dakika)
              </label>
              <input
                type="number"
                defaultValue="5"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );

  const renderNotificationSettings = () => (
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
              defaultValue="smtp.gmail.com"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              SMTP Port
            </label>
            <input
              type="number"
              defaultValue="587"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Gönderen E-posta
            </label>
            <input
              type="email"
              defaultValue="noreply@batulab.com"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Gönderen Adı
            </label>
            <input
              type="text"
              defaultValue="Office AI - Batu Lab"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Bildirim Türleri</h3>
        <div className="space-y-3">
          {[
            'Yeni kullanıcı kaydı',
            'Ödeme tamamlandı',
            'Ödeme başarısız',
            'Lisans süresi bitti',
            'Sistem hataları',
            'Güvenlik uyarıları'
          ].map((notification) => (
            <div key={notification} className="flex items-center space-x-3">
              <input
                type="checkbox"
                id={notification}
                defaultChecked
                className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              />
              <label htmlFor={notification} className="text-sm text-gray-700">
                {notification}
              </label>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

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
              defaultValue="pk_test_..."
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Secret Key
            </label>
            <input
              type="password"
              defaultValue="sk_test_..."
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Webhook Secret
            </label>
            <input
              type="password"
              defaultValue="whsec_..."
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
              defaultValue="29.99"
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
              defaultValue="299.99"
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
              defaultValue="999.99"
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
            defaultValue="1"
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
              defaultValue="sk-ant-..."
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Model
            </label>
            <select className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option value="claude-3-sonnet">Claude 3 Sonnet</option>
              <option value="claude-3-haiku">Claude 3 Haiku</option>
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
              defaultValue="AIza..."
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
              defaultValue="gsk_..."
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
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    {savedSection === activeSection && (
                      <div className="flex items-center text-green-600">
                        <CheckIcon className="w-4 h-4 mr-1" />
                        <span className="text-sm">Değişiklikler kaydedildi</span>
                      </div>
                    )}
                  </div>
                  <button
                    onClick={() => handleSave(activeSection)}
                    disabled={isSaving}
                    className="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isSaving ? (
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