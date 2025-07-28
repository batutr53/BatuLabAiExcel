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
      blue: 'bg-blue-50 hover:bg-blue-100 border-blue-200 text-blue-700',
      green: 'bg-green-50 hover:bg-green-100 border-green-200 text-green-700',
      yellow: 'bg-yellow-50 hover:bg-yellow-100 border-yellow-200 text-yellow-700',
      purple: 'bg-purple-50 hover:bg-purple-100 border-purple-200 text-purple-700',
    };
    return variants[color as keyof typeof variants] || variants.blue;
  };

  return (
    <div className="bg-white rounded-xl p-6 shadow-md border border-gray-200">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-900">Hızlı İşlemler</h2>
        <PlusIcon className="w-6 h-6 text-gray-400" />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {actions.map((action, index) => (
          <Link
            key={index}
            to={action.href}
            className={clsx(
              `p-5 rounded-lg border-2 border-dashed transition-all duration-200 hover:scale-105 hover:shadow-lg flex flex-col items-center text-center space-y-3`,
              getColorClasses(action.color)
            )}
          >
            <action.icon className="w-9 h-9" />
            <div>
              <h3 className="font-semibold text-base">{action.title}</h3>
              <p className="text-xs opacity-85 mt-1">{action.description}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}