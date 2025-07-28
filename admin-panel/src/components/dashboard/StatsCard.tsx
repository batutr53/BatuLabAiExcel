import React from 'react';
import { clsx } from 'clsx';

interface StatsCardProps {
  title: string;
  value: string | number;
  change: string;
  trend: 'up' | 'down';
  icon: React.ComponentType<{ className?: string }>;
  color: 'blue' | 'green' | 'yellow' | 'purple' | 'red';
}

const colorVariants = {
  blue: {
    bg: 'bg-blue-50',
    iconBg: 'bg-blue-100',
    icon: 'text-blue-600',
    accent: 'text-blue-600',
  },
  green: {
    bg: 'bg-green-50',
    iconBg: 'bg-green-100',
    icon: 'text-green-600',
    accent: 'text-green-600',
  },
  yellow: {
    bg: 'bg-yellow-50',
    iconBg: 'bg-yellow-100',
    icon: 'text-yellow-600',
    accent: 'text-yellow-600',
  },
  purple: {
    bg: 'bg-purple-50',
    iconBg: 'bg-purple-100',
    icon: 'text-purple-600',
    accent: 'text-purple-600',
  },
  red: {
    bg: 'bg-red-50',
    iconBg: 'bg-red-100',
    icon: 'text-red-600',
    accent: 'text-red-600',
  },
};

export function StatsCard({ title, value, change, trend, icon: Icon, color }: StatsCardProps) {
  const colors = colorVariants[color];

  return (
    <div className={clsx('rounded-lg p-6 shadow-sm border border-gray-100 transition-all hover:shadow-md', colors.bg)}>
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-600 mb-1">{title}</p>
          <p className="text-2xl font-bold text-gray-900 mb-2">{value}</p>
          <div className="flex items-center text-sm">
            <span
              className={clsx(
                'flex items-center font-medium',
                trend === 'up' ? 'text-green-600' : 'text-red-600'
              )}
            >
              {trend === 'up' ? (
                <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M3.293 9.707a1 1 0 010-1.414l6-6a1 1 0 011.414 0l6 6a1 1 0 01-1.414 1.414L11 5.414V17a1 1 0 11-2 0V5.414L4.707 9.707a1 1 0 01-1.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              ) : (
                <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 10.293a1 1 0 010 1.414l-6 6a1 1 0 01-1.414 0l-6-6a1 1 0 111.414-1.414L9 14.586V3a1 1 0 012 0v11.586l4.293-4.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              )}
              {change}
            </span>
            <span className="text-gray-500 ml-2">bu aydan beri</span>
          </div>
        </div>
        <div className={clsx('p-3 rounded-full', colors.iconBg)}>
          <Icon className={clsx('w-6 h-6', colors.icon)} />
        </div>
      </div>
    </div>
  );
}