import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ChartBarIcon, UsersIcon, DocumentTextIcon, CurrencyDollarIcon } from '@heroicons/react/24/outline';
import { analyticsAPI } from '../../services/api';
import { RevenueChart } from '../dashboard/RevenueChart';
import { UserGrowthChart } from '../dashboard/UserGrowthChart';
import { clsx } from 'clsx';

interface LicenseDistribution {
  type: string;
  count: number;
  percentage: number;
}

export function AnalyticsPage() {
  const { data: licenseDistributionData, isLoading: licenseDistributionLoading, error: licenseDistributionError } = useQuery<{
    data: LicenseDistribution[];
  }>({ // Explicitly define the type of data expected
    queryKey: ['license-distribution'],
    queryFn: () => analyticsAPI.getLicenseDistribution(),
  });

  if (licenseDistributionError) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Lisans dağılımı yüklenirken hata oluştu</p>
        </div>
      </div>
    );
  }

  const licenseDistribution = licenseDistributionData?.data || [];

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Analitik ve Raporlar</h1>
        <p className="text-gray-600">Sistem performansını, kullanıcı büyümesini ve gelir metriklerini takip edin.</p>
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <RevenueChart />
        <UserGrowthChart />
      </div>

      {/* License Distribution */}
      <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
        <div className="flex items-center space-x-2 mb-6">
          <DocumentTextIcon className="w-6 h-6 text-gray-400" />
          <h3 className="text-xl font-semibold text-gray-900">Lisans Dağılımı</h3>
        </div>
        {licenseDistributionLoading ? (
          <div className="h-48 flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
          </div>
        ) : licenseDistribution.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <DocumentTextIcon className="w-14 h-14 mx-auto mb-3 opacity-50" />
            <p className="text-lg">Lisans dağılım verisi bulunamadı.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {licenseDistribution.map((item, index) => (
              <div key={index} className="bg-gray-50 p-4 rounded-lg border border-gray-100 flex items-center space-x-4">
                <div className="flex-shrink-0 w-12 h-12 bg-primary-100 rounded-full flex items-center justify-center">
                  <DocumentTextIcon className="w-6 h-6 text-primary-600" />
                </div>
                <div>
                  <p className="text-lg font-semibold text-gray-900">{item.type}</p>
                  <p className="text-sm text-gray-600">{item.count} Lisans ({item.percentage}%)</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Other potential analytics sections can be added here */}
    </div>
  );
}
