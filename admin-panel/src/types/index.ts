// Export all types from their respective files
export * from './auth';
export * from './user';
export * from './license';
export * from './payment';
export * from './dashboard';
export * from './api';

// Notification Types
export interface Notification {
  id: string;
  title: string;
  message: string;
  type: string;
  createdAt: string;
  isRead: boolean;
}

// Settings Types
export interface AdminSettings {
  general: GeneralSettings;
  users: UserSettings;
  security: SecuritySettings;
  notifications: NotificationSettings;
  payment: PaymentSettings;
  api: ApiSettings;
}

export interface GeneralSettings {
  appName: string;
  version: string;
  timeZone: string;
  language: string;
  maintenanceMode: boolean;
}

export interface UserSettings {
  allowRegistration: boolean;
  emailVerificationRequired: boolean;
  autoTrialLicense: boolean;
  minimumPasswordLength: number;
  maxLoginAttempts: number;
}

export interface SecuritySettings {
  tokenExpiryHours: number;
  refreshTokenDurationDays: number;
  enableRateLimiting: boolean;
  generalRateLimit: number;
  authRateLimit: number;
  paymentRateLimit: number;
}

export interface NotificationSettings {
  smtpHost: string;
  smtpPort: number;
  smtpUsername: string;
  smtpPassword: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  enabledNotificationTypes: string[];
}

export interface PaymentSettings {
  stripePublishableKey: string;
  stripeSecretKey: string;
  stripeWebhookSecret: string;
  monthlyPlanPrice: number;
  yearlyPlanPrice: number;
  lifetimePlanPrice: number;
  trialDurationDays: number;
}

export interface ApiSettings {
  claudeApiKey: string;
  claudeModel: string;
  geminiApiKey: string;
  groqApiKey: string;
}

// Table Types
export interface TableColumn<T> {
  key: keyof T;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, item: T) => React.ReactNode;
}