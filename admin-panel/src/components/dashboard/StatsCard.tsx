import { clsx } from 'clsx';

export interface StatsCardProps {
  title: string;
  value: string | number;
  change: string;
  trend: 'up' | 'down';
  icon: React.ComponentType<{ className?: string }>;
  color: 'blue' | 'green' | 'yellow' | 'purple' | 'red';
  description?: string;
}

const colorVariants = {
  blue: {
    bg: 'bg-blue-50 dark:bg-blue-900/20',
    iconBg: 'bg-blue-100 dark:bg-blue-800/30',
    icon: 'text-blue-600 dark:text-blue-400',
    accent: 'text-blue-600 dark:text-blue-400',
  },
  green: {
    bg: 'bg-green-50 dark:bg-green-900/20',
    iconBg: 'bg-green-100 dark:bg-green-800/30',
    icon: 'text-green-600 dark:text-green-400',
    accent: 'text-green-600 dark:text-green-400',
  },
  yellow: {
    bg: 'bg-yellow-50 dark:bg-yellow-900/20',
    iconBg: 'bg-yellow-100 dark:bg-yellow-800/30',
    icon: 'text-yellow-600 dark:text-yellow-400',
    accent: 'text-yellow-600 dark:text-yellow-400',
  },
  purple: {
    bg: 'bg-purple-50 dark:bg-purple-900/20',
    iconBg: 'bg-purple-100 dark:bg-purple-800/30',
    icon: 'text-purple-600 dark:text-purple-400',
    accent: 'text-purple-600 dark:text-purple-400',
  },
  red: {
    bg: 'bg-red-50 dark:bg-red-900/20',
    iconBg: 'bg-red-100 dark:bg-red-800/30',
    icon: 'text-red-600 dark:text-red-400',
    accent: 'text-red-600 dark:text-red-400',
  },
};

export function StatsCard({ title, value, change, trend, icon: Icon, color, description }: StatsCardProps) {
  const colors = colorVariants[color];

  return (
    <div className="bg-white dark:bg-gray-800 rounded-2xl p-6 shadow-sm border border-gray-100 dark:border-gray-700 transition-all duration-300 hover:shadow-lg hover:scale-[1.02] group">
      <div className="flex items-start justify-between mb-4">
        <div className={clsx('p-3 rounded-xl group-hover:scale-110 transition-transform duration-200', colors.iconBg)}>
          <Icon className={clsx('w-6 h-6', colors.icon)} />
        </div>
        <div className="text-right">
          <span
            className={clsx(
              'inline-flex items-center px-2 py-1 rounded-full text-xs font-medium',
              trend === 'up' 
                ? 'bg-success-100 dark:bg-success-900/30 text-success-700 dark:text-success-400' 
                : 'bg-danger-100 dark:bg-danger-900/30 text-danger-700 dark:text-danger-400'
            )}
          >
            {trend === 'up' ? (
              <svg className="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M3.293 9.707a1 1 0 010-1.414l6-6a1 1 0 011.414 0l6 6a1 1 0 01-1.414 1.414L11 5.414V17a1 1 0 11-2 0V5.414L4.707 9.707a1 1 0 01-1.414 0z"
                  clipRule="evenodd"
                />
              </svg>
            ) : (
              <svg className="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M16.707 10.293a1 1 0 010 1.414l-6 6a1 1 0 01-1.414 0l-6-6a1 1 0 111.414-1.414L9 14.586V3a1 1 0 012 0v11.586l4.293-4.293a1 1 0 011.414 0z"
                  clipRule="evenodd"
                />
              </svg>
            )}
            {change}
          </span>
        </div>
      </div>

      <div>
        <h3 className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">{title}</h3>
        <p className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">{value}</p>
        {description && (
          <p className="text-xs text-gray-500 dark:text-gray-400">{description}</p>
        )}
      </div>
    </div>
  );
}
