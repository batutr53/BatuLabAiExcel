
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Toaster } from 'react-hot-toast';

import { AuthProvider } from './contexts/AuthContext';
import { LoginPage } from './components/auth/LoginPage';
import { DashboardLayout } from './components/layout/DashboardLayout';
import { DashboardPage } from './components/dashboard/DashboardPage';

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 2,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <Router>
          <div className="App">
            <Routes>
              {/* Public Routes */}
              <Route path="/login" element={<LoginPage />} />
              
              {/* Protected Routes */}
              <Route path="/" element={<DashboardLayout />}>
                <Route index element={<Navigate to="/dashboard" replace />} />
                <Route path="dashboard" element={<DashboardPage />} />
                
                {/* User Management */}
                <Route path="users" element={<div className="p-6"><h1 className="text-2xl font-bold">Kullanıcı Yönetimi</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                <Route path="users/new" element={<div className="p-6"><h1 className="text-2xl font-bold">Yeni Kullanıcı</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                
                {/* License Management */}
                <Route path="licenses" element={<div className="p-6"><h1 className="text-2xl font-bold">Lisans Yönetimi</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                <Route path="licenses/new" element={<div className="p-6"><h1 className="text-2xl font-bold">Yeni Lisans</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                
                {/* Payment Management */}
                <Route path="payments" element={<div className="p-6"><h1 className="text-2xl font-bold">Ödeme Yönetimi</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                
                {/* Analytics */}
                <Route path="analytics" element={<div className="p-6"><h1 className="text-2xl font-bold">Analitik</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                
                {/* Notifications */}
                <Route path="notifications" element={<div className="p-6"><h1 className="text-2xl font-bold">Bildirimler</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                <Route path="notifications/send" element={<div className="p-6"><h1 className="text-2xl font-bold">Bildirim Gönder</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
                
                {/* Settings */}
                <Route path="settings" element={<div className="p-6"><h1 className="text-2xl font-bold">Ayarlar</h1><p className="text-gray-600 mt-2">Bu sayfa geliştirilme aşamasında...</p></div>} />
              </Route>
              
              {/* Catch all */}
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </div>
        </Router>
      </AuthProvider>

      {/* Global Toast Notifications */}
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#fff',
            color: '#374151',
            border: '1px solid #e5e7eb',
            borderRadius: '8px',
            boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
          },
          success: {
            iconTheme: {
              primary: '#10b981',
              secondary: '#fff',
            },
          },
          error: {
            iconTheme: {
              primary: '#ef4444',
              secondary: '#fff',
            },
          },
        }}
      />

      {/* React Query DevTools */}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}

export default App;