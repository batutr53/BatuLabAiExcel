import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { analyticsAPI } from '../../services/api';
import { UsersIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';

type Period = 'week' | 'month' | 'year';

export function UserGrowthChart() {
  const [selectedPeriod, setSelectedPeriod] = useState<Period>('month');

  const { data, isLoading, error } = useQuery({
    queryKey: ['user-growth', selectedPeriod],
    queryFn: () => analyticsAPI.getUserGrowthAnalytics(selectedPeriod),
    refetchInterval: 300000, // 5 minutes
  });

  const chartData = (data?.data || []) as Array<{ date: string; value: number }>;

  const periods = [
    { key: 'week' as Period, label: 'Haftalık' },
    { key: 'month' as Period, label: 'Aylık' },
    { key: 'year' as Period, label: 'Yıllık' },
  ];

  if (error) {
    return (
      <div className="bg-white rounded-xl p-6 shadow-md border border-gray-200">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-xl font-semibold text-gray-900">Kullanıcı Büyümesi</h3>
        </div>
        <div className="text-center py-8 text-red-500">
          <p>Veri yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100 hover:shadow-lg transition-all duration-300">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-2">
          <UsersIcon className="w-6 h-6 text-gray-400" />
          <h3 className="text-xl font-semibold text-gray-900">Kullanıcı Büyümesi</h3>
        </div>
        
        {/* Period Selector */}
        <div className="flex items-center space-x-1 bg-gray-100 rounded-lg p-1">
          {periods.map((period) => (
            <button
              key={period.key}
              onClick={() => setSelectedPeriod(period.key)}
              className={clsx(
                'px-3 py-1 text-sm font-medium rounded-md transition-colors',
                selectedPeriod === period.key
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              )}
            >
              {period.label}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      {isLoading ? (
        <div className="h-64 flex items-center justify-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        </div>
      ) : (
        <div className="h-64 group">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
              <defs>
                <linearGradient id="userGrowthGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis 
                dataKey="date" 
                stroke="#6b7280"
                fontSize={12}
                tickFormatter={(value: string) => {
                  const date = new Date(value);
                  if (selectedPeriod === 'week') {
                    return date.toLocaleDateString('tr-TR', { weekday: 'short' });
                  } else if (selectedPeriod === 'month') {
                    return date.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });
                  } else {
                    return date.toLocaleDateString('tr-TR', { month: 'short', year: '2-digit' });
                  }
                }}
              />
              <YAxis 
                stroke="#6b7280"
                fontSize={12}
                tickFormatter={(value: number) => value.toLocaleString()}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: 'white',
                  border: '1px solid #e5e7eb',
                  borderRadius: '8px',
                  boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
                }}
                labelFormatter={(value: string) => {
                  const date = new Date(value);
                  return date.toLocaleDateString('tr-TR', {
                    day: 'numeric',
                    month: 'long',
                    year: 'numeric',
                  });
                }}
                formatter={(value: number) => [value.toLocaleString(), 'Kullanıcı']}
              />
              <Area
                type="monotone"
                dataKey="value"
                stroke="#22c55e"
                strokeWidth={3}
                fill="url(#userGrowthGradient)"
                dot={{ fill: '#22c55e', strokeWidth: 2, r: 4 }}
                activeDot={{ r: 6, stroke: '#22c55e', strokeWidth: 2, fill: 'white' }}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Summary */}
      {!isLoading && chartData.length > 0 && (
        <div className="mt-6 pt-4 border-t border-gray-100">
          <div className="grid grid-cols-3 gap-4 text-center">
            <div>
              <p className="text-2xl font-bold text-gray-900">
                <span className="group-hover:text-primary-600 transition-colors">{Math.max(...chartData.map((d) => d.value)).toLocaleString()}</span>
              </p>
              <p className="text-xs text-gray-600">En Yüksek</p>
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">
                <span className="group-hover:text-success-600 transition-colors">{chartData.length > 1 
                  ? `+${(chartData[chartData.length - 1].value - chartData[0].value).toLocaleString()}`
                  : '0'
                }</span>
              </p>
              <p className="text-xs text-gray-600">Net Artış</p>
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">
                <span className="group-hover:text-warning-600 transition-colors">{chartData.length > 1 
                  ? `%${Math.round(((chartData[chartData.length - 1].value - chartData[0].value) / chartData[0].value) * 100)}`
                  : '0%'
                }</span>
              </p>
              <p className="text-xs text-gray-600">Büyüme Oranı</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
