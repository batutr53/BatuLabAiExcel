import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  EyeIcon,
  EyeSlashIcon,
  ArrowRightIcon,
  ShieldCheckIcon,
  ChartBarSquareIcon,
  UserGroupIcon,
  CogIcon,
  SparklesIcon,
} from '@heroicons/react/24/outline';
import { useAuth } from '../../contexts/AuthContext';
import toast from 'react-hot-toast';

// Form validation schema
const loginSchema = z.object({
  email: z.string().email('Geçerli bir email adresi girin'),
  password: z.string().min(6, 'Şifre en az 6 karakter olmalı'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const { state, login } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      toast.success('Başarıyla giriş yapıldı!');
      void navigate('/dashboard');
    } catch (error) {
      if (error instanceof Error) {
        toast.error(error.message);
      } else {
        toast.error('Giriş sırasında bilinmeyen bir hata oluştu.');
      }
    }
  };

  // Redirect if already authenticated
  useEffect(() => {
    if (state.isAuthenticated) {
      void navigate('/dashboard');
    }
  }, [state.isAuthenticated, navigate]);

  if (state.loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100">
        <div className="flex flex-col items-center space-y-4">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
          <p className="text-primary-600 font-medium">Yükleniyor...</p>
        </div>
      </div>
    );
  }

  const features = [
    {
      icon: ChartBarSquareIcon,
      title: 'Gelişmiş Analitik',
      description: 'Gerçek zamanlı veri analizi ve raporlama'
    },
    {
      icon: UserGroupIcon,
      title: 'Kullanıcı Yönetimi',
      description: 'Kapsamlı kullanıcı ve rol yönetimi'
    },
    {
      icon: ShieldCheckIcon,
      title: 'Güvenlik',
      description: 'Kurumsal seviye güvenlik ve şifreleme'
    },
    {
      icon: CogIcon,
      title: 'Özelleştirme',
      description: 'Esnek yapılandırma ve kişiselleştirme'
    }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 via-white to-secondary-50 flex">
      {/* Left Panel - Features */}
      <div className="hidden lg:flex w-1/2 bg-gradient-to-br from-primary-600 via-primary-700 to-primary-800 relative overflow-hidden">
        {/* Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-0 left-0 w-96 h-96 bg-white rounded-full -translate-x-1/2 -translate-y-1/2"></div>
          <div className="absolute top-1/2 right-0 w-64 h-64 bg-white rounded-full translate-x-1/2 -translate-y-1/2"></div>
          <div className="absolute bottom-0 left-1/3 w-80 h-80 bg-white rounded-full translate-y-1/2"></div>
        </div>
        
        <div className="relative z-10 flex flex-col justify-center p-12 text-white">
          {/* Logo & Brand */}
          <div className="mb-12">
            <div className="flex items-center space-x-3 mb-6">
              <div className="w-12 h-12 bg-white/20 rounded-2xl flex items-center justify-center backdrop-blur-sm">
                <SparklesIcon className="w-7 h-7 text-white" />
              </div>
              <div>
                <h1 className="text-2xl font-bold">Office AI</h1>
                <p className="text-primary-200 text-sm">Batu Lab Admin Panel</p>
              </div>
            </div>
            <h2 className="text-4xl font-bold leading-tight mb-4">
              Yönetim Paneline<br />
              <span className="text-primary-200">Hoş Geldiniz</span>
            </h2>
            <p className="text-xl text-primary-100 mb-8">
              Tüm operasyonlarınızı tek yerden yönetin. Modern, güvenli ve kullanıcı dostu arayüz.
            </p>
          </div>

          {/* Features Grid */}
          <div className="grid grid-cols-2 gap-6">
            {features.map((feature, index) => (
              <div 
                key={index}
                className="bg-white/10 backdrop-blur-sm p-6 rounded-2xl border border-white/20 hover:bg-white/15 transition-all duration-300 animate-slide-up"
                style={{ animationDelay: `${index * 0.1}s` }}
              >
                <feature.icon className="w-8 h-8 text-primary-200 mb-3" />
                <h3 className="font-semibold text-white mb-2">{feature.title}</h3>
                <p className="text-primary-200 text-sm">{feature.description}</p>
              </div>
            ))}
          </div>

          {/* Stats */}
          <div className="mt-12 flex items-center space-x-8">
            <div className="text-center">
              <div className="text-3xl font-bold text-white">99.9%</div>
              <div className="text-primary-200 text-sm">Uptime</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-white">50K+</div>
              <div className="text-primary-200 text-sm">Kullanıcı</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-white">24/7</div>
              <div className="text-primary-200 text-sm">Destek</div>
            </div>
          </div>
        </div>
      </div>

      {/* Right Panel - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-6 sm:p-12">
        <div className="max-w-md w-full">
          {/* Mobile Logo */}
          <div className="lg:hidden text-center mb-8">
            <div className="inline-flex items-center space-x-3 mb-4">
              <div className="w-10 h-10 bg-primary-600 rounded-xl flex items-center justify-center">
                <SparklesIcon className="w-6 h-6 text-white" />
              </div>
              <div className="text-left">
                <h1 className="text-xl font-bold text-gray-900">Office AI</h1>
                <p className="text-gray-500 text-xs">Batu Lab Admin</p>
              </div>
            </div>
          </div>

          {/* Form Header */}
          <div className="text-center mb-8">
            <h2 className="text-3xl font-bold text-gray-900 mb-2">
              Giriş Yapın
            </h2>
            <p className="text-gray-600">
              Yönetim paneline erişmek için hesap bilgilerinizi girin
            </p>
          </div>

          {/* Login Form */}
          <div className="bg-white rounded-3xl shadow-2xl shadow-primary-500/10 p-8 border border-gray-100">
            <form className="space-y-6" onSubmit={(e) => void handleSubmit(onSubmit)(e)}>
              {/* Email Field */}
              <div className="space-y-1">
                <label htmlFor="email" className="block text-sm font-semibold text-gray-700">
                  Email Adresi
                </label>
                <div className="relative">
                  <input
                    {...register('email')}
                    type="email"
                    autoComplete="email"
                    className="w-full px-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all duration-200 bg-gray-50 focus:bg-white"
                    placeholder="admin@batulab.com"
                  />
                </div>
                {errors.email && (
                  <p className="text-sm text-danger-600 mt-1">{errors.email.message}</p>
                )}
              </div>

              {/* Password Field */}
              <div className="space-y-1">
                <label htmlFor="password" className="block text-sm font-semibold text-gray-700">
                  Şifre
                </label>
                <div className="relative">
                  <input
                    {...register('password')}
                    type={showPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    className="w-full px-4 py-3 pr-12 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all duration-200 bg-gray-50 focus:bg-white"
                    placeholder="••••••••"
                  />
                  <button
                    type="button"
                    className="absolute inset-y-0 right-0 pr-4 flex items-center"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? (
                      <EyeSlashIcon className="h-5 w-5 text-gray-400 hover:text-gray-600 transition-colors" />
                    ) : (
                      <EyeIcon className="h-5 w-5 text-gray-400 hover:text-gray-600 transition-colors" />
                    )}
                  </button>
                </div>
                {errors.password && (
                  <p className="text-sm text-danger-600 mt-1">{errors.password.message}</p>
                )}
              </div>

              {/* Error Message */}
              {state.error && (
                <div className="bg-danger-50 border border-danger-200 rounded-xl p-4 animate-slide-down">
                  <div className="flex">
                    <div className="flex-shrink-0">
                      <div className="w-5 h-5 bg-danger-500 rounded-full flex items-center justify-center">
                        <span className="text-white text-xs font-bold">!</span>
                      </div>
                    </div>
                    <div className="ml-3">
                      <h3 className="text-sm font-medium text-danger-800">
                        Giriş Hatası
                      </h3>
                      <div className="mt-1 text-sm text-danger-700">
                        {state.error}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Submit Button */}
              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full bg-gradient-to-r from-primary-600 to-primary-700 hover:from-primary-700 hover:to-primary-800 text-white font-semibold py-3 px-4 rounded-xl transition-all duration-300 transform hover:scale-[1.02] focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none flex items-center justify-center space-x-2 shadow-lg hover:shadow-xl"
              >
                {isSubmitting ? (
                  <div className="flex items-center space-x-2">
                    <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
                    <span>Giriş yapılıyor...</span>
                  </div>
                ) : (
                  <>
                    <span>Giriş Yap</span>
                    <ArrowRightIcon className="h-5 w-5 transition-transform group-hover:translate-x-1" />
                  </>
                )}
              </button>
            </form>

            {/* Demo Credentials */}
            <div className="mt-6 p-4 bg-gradient-to-r from-primary-50 to-secondary-50 rounded-xl border border-primary-100">
              <h4 className="text-sm font-semibold text-gray-700 mb-2 flex items-center">
                <SparklesIcon className="w-4 h-4 mr-2 text-primary-600" />
                Demo Giriş Bilgileri
              </h4>
              <div className="text-sm text-gray-600 space-y-1">
                <p><span className="font-medium">Email:</span> admin@batulab.com</p>
                <p><span className="font-medium">Şifre:</span> admin123</p>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="text-center mt-8">
            <p className="text-sm text-gray-500">
              © 2025 Office AI - Batu Lab. Tüm hakları saklıdır.
            </p>
            <div className="flex items-center justify-center space-x-4 mt-2">
              <a href="#" className="text-xs text-gray-400 hover:text-primary-600 transition-colors">Gizlilik</a>
              <span className="text-gray-300">•</span>
              <a href="#" className="text-xs text-gray-400 hover:text-primary-600 transition-colors">Şartlar</a>
              <span className="text-gray-300">•</span>
              <a href="#" className="text-xs text-gray-400 hover:text-primary-600 transition-colors">Destek</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}