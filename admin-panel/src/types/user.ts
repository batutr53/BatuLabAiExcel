import type { License } from './license';

// User Management Types
export interface User {
  id: string;
  email: string;
  name?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  licenseInfo?: License;
}