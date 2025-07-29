
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Toaster } from 'react-hot-toast';

import { AuthProvider } from './contexts/AuthContext';
import { LoginPage } from './components/auth/LoginPage';
import { DashboardLayout } from './components/layout/DashboardLayout';
import { DashboardPage } from './components/dashboard/DashboardPage';
import { UsersPage } from './components/users/UsersPage';
import { UserFormPage } from './components/users/UserFormPage';
import { LicensesPage } from './components/licenses/LicensesPage';
import { LicenseFormPage } from './components/licenses/LicenseFormPage';
import { PaymentsPage } from './components/payments/PaymentsPage';
import { AnalyticsPage } from './components/analytics/AnalyticsPage';
import { NotificationsPage } from './components/notifications/NotificationsPage';
import { SendNotificationPage } from './components/notifications/SendNotificationPage';
import { SettingsPage } from './components/settings/SettingsPage';

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
                <Route path="users" element={<UsersPage />} />
                <Route path="users/new" element={<UserFormPage />} />
                
                {/* License Management */}
                <Route path="licenses" element={<LicensesPage />} />
                <Route path="licenses/new" element={<LicenseFormPage />} />
                
                {/* Payment Management */}
                <Route path="payments" element={<PaymentsPage />} />
                
                {/* Analytics */}
                <Route path="analytics" element={<AnalyticsPage />} />
                
                {/* Notifications */}
                <Route path="notifications" element={<NotificationsPage />} />
                <Route path="notifications/send" element={<SendNotificationPage />} />
                
                {/* Settings */}
                <Route path="settings" element={<SettingsPage />} />
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