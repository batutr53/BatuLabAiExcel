import { Fragment } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { NavLink } from 'react-router-dom';
import {
  XMarkIcon,
  HomeIcon,
  UsersIcon,
  DocumentTextIcon,
  CreditCardIcon,
  ChartBarIcon,
  CogIcon,
  ShieldCheckIcon,
  BellIcon,
  SparklesIcon,
  CommandLineIcon,
  CircleStackIcon,
  GlobeAltIcon,
} from '@heroicons/react/24/outline';
import { clsx } from 'clsx';
import { useTheme } from '../../contexts/ThemeContext';

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

const navigation = [
  {
    name: 'Dashboard',
    href: '/dashboard',
    icon: HomeIcon,
    description: 'Genel bakış ve istatistikler'
  },
  {
    name: 'Kullanıcılar',
    href: '/users',
    icon: UsersIcon,
    description: 'Kullanıcı yönetimi ve profiller'
  },
  {
    name: 'Lisanslar',
    href: '/licenses',
    icon: DocumentTextIcon,
    description: 'Lisans yönetimi ve atama'
  },
  {
    name: 'Ödemeler',
    href: '/payments',
    icon: CreditCardIcon,
    description: 'Ödeme geçmişi ve faturalandırma'
  },
  {
    name: 'Analitik',
    href: '/analytics',
    icon: ChartBarIcon,
    description: 'Detaylı raporlar ve analizler'
  },
  {
    name: 'Sistem',
    href: '/system',
    icon: CircleStackIcon,
    description: 'Sistem durumu ve loglar'
  },
];

const secondaryNavigation = [
  {
    name: 'Bildirimler',
    href: '/notifications',
    icon: BellIcon,
  },
  {
    name: 'API Yönetimi',
    href: '/api',
    icon: CommandLineIcon,
  },
  {
    name: 'Güvenlik',
    href: '/security',
    icon: ShieldCheckIcon,
  },
  {
    name: 'Web Sitesi',
    href: '#',
    icon: GlobeAltIcon,
    external: true,
  },
];

function SidebarContent() {
  return (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="flex items-center h-16 px-6 border-b border-gray-200/50 dark:border-gray-700/50">
        <div className="flex items-center space-x-3">
          <div className="w-8 h-8 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl flex items-center justify-center shadow-lg">
            <SparklesIcon className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-gray-900 dark:text-gray-100">Office AI</h1>
            <p className="text-xs text-gray-500 dark:text-gray-400">Batu Lab Admin</p>
          </div>
        </div>
      </div>

      {/* Main Navigation */}
      <div className="flex-1 px-3 py-6 space-y-1 overflow-y-auto">
        <div className="space-y-1">
          {navigation.map((item) => (
            <NavLink
              key={item.name}
              to={item.href}
              className={({ isActive }) =>
                clsx(
                  'group flex items-center px-3 py-3 text-sm font-medium rounded-xl transition-all duration-200 relative',
                  isActive
                    ? 'bg-gradient-to-r from-primary-500 to-primary-600 text-white shadow-lg shadow-primary-500/25'
                    : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-gray-100'
                )
              }
            >
              {({ isActive }) => (
                <>
                  <item.icon
                    className={clsx(
                      'flex-shrink-0 w-5 h-5 mr-3 transition-colors',
                      isActive ? 'text-white' : 'text-gray-400 dark:text-gray-500 group-hover:text-gray-500 dark:group-hover:text-gray-400'
                    )}
                  />
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">{item.name}</div>
                    <div className={clsx(
                      'text-xs mt-0.5 truncate',
                      isActive ? 'text-primary-100' : 'text-gray-500'
                    )}>
                      {item.description}
                    </div>
                  </div>
                  {isActive && (
                    <div className="absolute right-0 top-1/2 transform -translate-y-1/2 w-1 h-8 bg-white rounded-l-full opacity-80" />
                  )}
                </>
              )}
            </NavLink>
          ))}
        </div>

        {/* Divider */}
        <div className="pt-6 mt-6 border-t border-gray-200/50">
          <div className="space-y-1">
            <h3 className="px-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">
              Araçlar
            </h3>
            {secondaryNavigation.map((item) => (
              <NavLink
                key={item.name}
                to={item.href}
                className={({ isActive }) =>
                  clsx(
                    'group flex items-center px-3 py-2 text-sm font-medium rounded-xl transition-all duration-200',
                    isActive
                      ? 'bg-primary-50 text-primary-700 border border-primary-200'
                      : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                  )
                }
              >
                <item.icon className="flex-shrink-0 w-4 h-4 mr-3 text-gray-400 group-hover:text-gray-500" />
                {item.name}
                {item.external && (
                  <svg className="w-3 h-3 ml-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                )}
              </NavLink>
            ))}
          </div>
        </div>
      </div>

      {/* Bottom Section */}
      <div className="px-3 py-4 border-t border-gray-200/50">
        <NavLink
          to="/settings"
          className={({ isActive }) =>
            clsx(
              'group flex items-center px-3 py-2 text-sm font-medium rounded-xl transition-all duration-200',
              isActive
                ? 'bg-primary-50 text-primary-700 border border-primary-200'
                : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
            )
          }
        >
          <CogIcon className="flex-shrink-0 w-4 h-4 mr-3 text-gray-400 group-hover:text-gray-500" />
          Ayarlar
        </NavLink>
        
        {/* Version Info */}
        <div className="mt-4 px-3 py-2 bg-gray-50 rounded-xl">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-xs font-medium text-gray-900">v2.1.0</div>
              <div className="text-xs text-gray-500">Güncel</div>
            </div>
            <div className="w-2 h-2 bg-success-500 rounded-full animate-pulse"></div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function Sidebar({ open, onClose }: SidebarProps) {
  return (
    <>
      {/* Mobile sidebar */}
      <Transition.Root show={open} as={Fragment}>
        <Dialog as="div" className="relative z-50 lg:hidden" onClose={onClose}>
          <Transition.Child
            as={Fragment}
            enter="transition-opacity ease-linear duration-300"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="transition-opacity ease-linear duration-300"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-gray-900/80 backdrop-blur-sm" />
          </Transition.Child>

          <div className="fixed inset-0 flex">
            <Transition.Child
              as={Fragment}
              enter="transition ease-in-out duration-300 transform"
              enterFrom="-translate-x-full"
              enterTo="translate-x-0"
              leave="transition ease-in-out duration-300 transform"
              leaveFrom="translate-x-0"
              leaveTo="-translate-x-full"
            >
              <Dialog.Panel className="relative mr-16 flex w-full max-w-xs flex-1">
                <Transition.Child
                  as={Fragment}
                  enter="ease-in-out duration-300"
                  enterFrom="opacity-0"
                  enterTo="opacity-100"
                  leave="ease-in-out duration-300"
                  leaveFrom="opacity-100"
                  leaveTo="opacity-0"
                >
                  <div className="absolute left-full top-0 flex w-16 justify-center pt-5">
                    <button type="button" className="-m-2.5 p-2.5" onClick={onClose}>
                      <span className="sr-only">Close sidebar</span>
                      <XMarkIcon className="h-6 w-6 text-white" aria-hidden="true" />
                    </button>
                  </div>
                </Transition.Child>
                <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 shadow-xl">
                  <SidebarContent />
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </Dialog>
      </Transition.Root>

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:z-40 lg:flex lg:w-72 lg:flex-col">
        <div className="flex grow flex-col overflow-y-auto bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 shadow-sm">
          <SidebarContent />
        </div>
      </div>
    </>
  );
}