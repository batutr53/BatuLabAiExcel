import { useState, Fragment } from 'react';
import { Menu, Transition, Popover } from '@headlessui/react';
import { 
  Bars3Icon,
  BellIcon,
  MagnifyingGlassIcon,
  UserCircleIcon,
  ArrowRightOnRectangleIcon,
  CogIcon,
  SunIcon,
  MoonIcon,
  SparklesIcon,
  ChartBarIcon,
  UsersIcon,
  CommandLineIcon,
} from '@heroicons/react/24/outline';
import { useAuth } from '../../contexts/AuthContext';
import toast from 'react-hot-toast';
import { clsx } from 'clsx';

interface HeaderProps {
  onMenuClick: () => void;
}

export function Header({ onMenuClick }: HeaderProps) {
  const { state, logout } = useAuth();
  const [searchQuery, setSearchQuery] = useState('');
  const [isDarkMode, setIsDarkMode] = useState(false);

  const handleLogout = async () => {
    try {
      await logout();
      toast.success('Başarıyla çıkış yapıldı');
    } catch {
      toast.error('Çıkış yapılırken hata oluştu');
    }
  };

  const notifications = [
    {
      id: 1,
      title: 'Yeni kullanıcı kaydı',
      message: '5 yeni kullanıcı sisteme kayıt oldu',
      time: '2 dakika önce',
      type: 'info',
      unread: true,
    },
    {
      id: 2,
      title: 'Sistem güncellemesi',
      message: 'v2.1.0 güncellemesi başarıyla tamamlandı',
      time: '1 saat önce',
      type: 'success',
      unread: true,
    },
    {
      id: 3,
      title: 'Ödeme alındı',
      message: '₺2,500 tutarında ödeme işlendi',
      time: '3 saat önce',
      type: 'success',
      unread: false,
    },
  ];

  const quickActions = [
    { name: 'Yeni Kullanıcı', icon: UsersIcon, href: '/users/new' },
    { name: 'Sistem Durumu', icon: ChartBarIcon, href: '/system' },
    { name: 'API Anahtarları', icon: CommandLineIcon, href: '/api/keys' },
  ];

  const unreadCount = notifications.filter(n => n.unread).length;

  return (
    <div className="bg-white border-b border-gray-200 shadow-sm sticky top-0 z-30">
      <div className="px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Left side */}
          <div className="flex items-center space-x-4">
            {/* Mobile menu button */}
            <button
              type="button"
              className="lg:hidden p-2 rounded-xl text-gray-400 hover:text-gray-500 hover:bg-gray-50 transition-all duration-200"
              onClick={onMenuClick}
            >
              <Bars3Icon className="h-6 w-6" />
            </button>

            {/* Search */}
            <div className="hidden sm:block">
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <MagnifyingGlassIcon className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  type="text"
                  placeholder="Ara..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="block w-full pl-10 pr-3 py-2 border border-gray-200 rounded-xl bg-gray-50 focus:bg-white focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm transition-all duration-200 placeholder-gray-500"
                />
                {searchQuery && (
                  <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-xl shadow-lg z-50">
                    <div className="p-3">
                      <div className="text-sm text-gray-500 mb-2">Öneriler</div>
                      {quickActions.map((action) => (
                        <a
                          key={action.name}
                          href={action.href}
                          className="flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-lg transition-colors"
                        >
                          <action.icon className="w-4 h-4 mr-3 text-gray-400" />
                          {action.name}
                        </a>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Right side */}
          <div className="flex items-center space-x-2">
            {/* Theme toggle */}
            <button
              onClick={() => setIsDarkMode(!isDarkMode)}
              className="p-2 rounded-xl text-gray-400 hover:text-gray-500 hover:bg-gray-50 transition-all duration-200"
            >
              {isDarkMode ? (
                <SunIcon className="h-5 w-5" />
              ) : (
                <MoonIcon className="h-5 w-5" />
              )}
            </button>

            {/* Notifications */}
            <Popover className="relative">
              <Popover.Button className="p-2 rounded-xl text-gray-400 hover:text-gray-500 hover:bg-gray-50 transition-all duration-200 relative">
                <BellIcon className="h-5 w-5" />
                {unreadCount > 0 && (
                  <span className="absolute -top-1 -right-1 h-4 w-4 bg-danger-500 text-white text-xs rounded-full flex items-center justify-center animate-pulse">
                    {unreadCount}
                  </span>
                )}
              </Popover.Button>

              <Transition
                as={Fragment}
                enter="transition ease-out duration-200"
                enterFrom="opacity-0 translate-y-1"
                enterTo="opacity-100 translate-y-0"
                leave="transition ease-in duration-150"
                leaveFrom="opacity-100 translate-y-0"
                leaveTo="opacity-0 translate-y-1"
              >
                <Popover.Panel className="absolute right-0 z-50 mt-2 w-80 bg-white rounded-2xl shadow-2xl border border-gray-100">
                  <div className="p-4">
                    <div className="flex items-center justify-between mb-4">
                      <h3 className="text-lg font-semibold text-gray-900">Bildirimler</h3>
                      {unreadCount > 0 && (
                        <span className="text-sm text-primary-600 font-medium">
                          {unreadCount} yeni
                        </span>
                      )}
                    </div>
                    <div className="space-y-3 max-h-96 overflow-y-auto">
                      {notifications.map((notification) => (
                        <div
                          key={notification.id}
                          className={clsx(
                            'p-3 rounded-xl border transition-all duration-200 hover:shadow-sm cursor-pointer',
                            notification.unread
                              ? 'bg-primary-50 border-primary-200'
                              : 'bg-gray-50 border-gray-200'
                          )}
                        >
                          <div className="flex items-start">
                            <div className={clsx(
                              'flex-shrink-0 w-2 h-2 rounded-full mt-2 mr-3',
                              notification.type === 'success' ? 'bg-success-500' :
                              notification.type === 'info' ? 'bg-primary-500' :
                              'bg-warning-500'
                            )} />
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium text-gray-900 truncate">
                                {notification.title}
                              </p>
                              <p className="text-sm text-gray-600 mt-1">
                                {notification.message}
                              </p>
                              <p className="text-xs text-gray-500 mt-2">
                                {notification.time}
                              </p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                    <div className="mt-4 pt-3 border-t border-gray-200">
                      <button className="w-full text-sm text-primary-600 hover:text-primary-700 font-medium">
                        Tüm bildirimleri görüntüle
                      </button>
                    </div>
                  </div>
                </Popover.Panel>
              </Transition>
            </Popover>

            {/* Profile dropdown */}
            <Menu as="div" className="relative">
              <Menu.Button className="flex items-center space-x-3 p-2 rounded-xl hover:bg-gray-50 transition-all duration-200">
                <div className="w-8 h-8 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl flex items-center justify-center shadow-sm">
                  <SparklesIcon className="w-4 h-4 text-white" />
                </div>
                <div className="hidden sm:block text-left">
                  <div className="text-sm font-medium text-gray-900">
                    {state.user?.name || state.user?.email || 'Admin'}
                  </div>
                  <div className="text-xs text-gray-500">Administrator</div>
                </div>
                <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </Menu.Button>

              <Transition
                as={Fragment}
                enter="transition ease-out duration-200"
                enterFrom="opacity-0 scale-95"
                enterTo="opacity-100 scale-100"
                leave="transition ease-in duration-75"
                leaveFrom="opacity-100 scale-100"
                leaveTo="opacity-0 scale-95"
              >
                <Menu.Items className="absolute right-0 z-50 mt-2 w-56 bg-white rounded-2xl shadow-2xl border border-gray-100">
                  <div className="p-2">
                    {/* Profile info */}
                    <div className="px-3 py-2 mb-2 bg-gray-50 rounded-xl">
                      <div className="text-sm font-medium text-gray-900">
                        {state.user?.name || 'Admin User'}
                      </div>
                      <div className="text-xs text-gray-500">
                        {state.user?.email || 'admin@batulab.com'}
                      </div>
                    </div>

                    {/* Menu items */}
                    <Menu.Item>
                      {({ active }) => (
                        <button
                          className={clsx(
                            'flex w-full items-center px-3 py-2 text-sm rounded-xl transition-all duration-200',
                            active ? 'bg-primary-50 text-primary-700' : 'text-gray-700'
                          )}
                        >
                          <UserCircleIcon className="mr-3 h-4 w-4" />
                          Profil
                        </button>
                      )}
                    </Menu.Item>

                    <Menu.Item>
                      {({ active }) => (
                        <button
                          className={clsx(
                            'flex w-full items-center px-3 py-2 text-sm rounded-xl transition-all duration-200',
                            active ? 'bg-primary-50 text-primary-700' : 'text-gray-700'
                          )}
                        >
                          <CogIcon className="mr-3 h-4 w-4" />
                          Ayarlar
                        </button>
                      )}
                    </Menu.Item>

                    <div className="border-t border-gray-200 my-2"></div>

                    <Menu.Item>
                      {({ active }) => (
                        <button
                          onClick={() => void handleLogout()}
                          className={clsx(
                            'flex w-full items-center px-3 py-2 text-sm rounded-xl transition-all duration-200',
                            active ? 'bg-danger-50 text-danger-700' : 'text-gray-700'
                          )}
                        >
                          <ArrowRightOnRectangleIcon className="mr-3 h-4 w-4" />
                          Çıkış Yap
                        </button>
                      )}
                    </Menu.Item>
                  </div>
                </Menu.Items>
              </Transition>
            </Menu>
          </div>
        </div>
      </div>
    </div>
  );
}