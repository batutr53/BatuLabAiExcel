import { Link } from 'react-router-dom';
import { 
  PlusIcon,
  UserPlusIcon,
  DocumentPlusIcon,
  BellIcon,
  AdjustmentsHorizontalIcon
} from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

export function QuickActions() {
  const actions = [
    {
      title: 'Yeni Kullanıcı',
      description: 'Manuel kullanıcı ekle',
      icon: UserPlusIcon,
      href: '/users/new',
      color: 'blue',
    },
    {
      title: 'Lisans Oluştur',
      description: 'Özel lisans tanımla',
      icon: DocumentPlusIcon,
      href: '/licenses/new',
      color: 'green',
    },
    {
      title: 'Bildirim Gönder',
      description: 'Toplu bildirim yayınla',
      icon: BellIcon,
      href: '/notifications/send',
      color: 'yellow',
    },
    {
      title: 'Sistem Ayarları',
      description: 'Genel konfigürasyon',
      icon: AdjustmentsHorizontalIcon,
      href: '/settings',
      color: 'purple',
    },
  ];

  const getColorClasses = (color: string) => {
    const variants = {
      blue: 'bg-blue-50 hover:bg-blue-100 border-blue-200 text-blue-700 dark:bg-blue-900/20 dark:hover:bg-blue-900/30 dark:border-blue-800 dark:text-blue-400',
      green: 'bg-green-50 hover:bg-green-100 border-green-200 text-green-700 dark:bg-green-900/20 dark:hover:bg-green-900/30 dark:border-green-800 dark:text-green-400',
      yellow: 'bg-yellow-50 hover:bg-yellow-100 border-yellow-200 text-yellow-700 dark:bg-yellow-900/20 dark:hover:bg-yellow-900/30 dark:border-yellow-800 dark:text-yellow-400',
      purple: 'bg-purple-50 hover:bg-purple-100 border-purple-200 text-purple-700 dark:bg-purple-900/20 dark:hover:bg-purple-900/30 dark:border-purple-800 dark:text-purple-400',
    };
    return variants[color as keyof typeof variants] || variants.blue;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Hızlı İşlemler</h2>
        <div className="w-8 h-8 bg-primary-100 rounded-lg flex items-center justify-center">
          <PlusIcon className="w-4 h-4 text-primary-600" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        {actions.map((action, index) => (
          <Link
            key={index}
            to={action.href}
            className={clsx(
              'group p-4 rounded-xl border-2 transition-all duration-300 hover:scale-105 hover:shadow-md flex flex-col items-center text-center space-y-3 hover:border-primary-200 dark:hover:border-primary-600',
              getColorClasses(action.color)
            )}
          >
            <div className="w-12 h-12 rounded-lg bg-white/50 dark:bg-gray-800/50 flex items-center justify-center group-hover:bg-white/80 dark:group-hover:bg-gray-800/80 transition-colors">
              <action.icon className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-semibold text-sm group-hover:text-gray-900 dark:group-hover:text-white transition-colors">{action.title}</h3>
              <p className="text-xs opacity-75 mt-1 group-hover:opacity-90 transition-opacity">{action.description}</p>
            </div>
          </Link>
        ))}
      </div>

      <div className="mt-6 pt-4 border-t border-gray-100 dark:border-gray-700">
        <Link 
          to="/analytics"
          className="w-full flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-600 hover:text-primary-700 bg-primary-50 hover:bg-primary-100 dark:text-primary-400 dark:bg-primary-900/20 dark:hover:bg-primary-900/30 rounded-lg transition-colors"
        >
          Tüm İşlemleri Görüntüle
        </Link>
      </div>
    </div>
  );
}