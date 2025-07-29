import type { License } from './license';

// User Management Types
export interface User {
  id: string;
  email: string;
  name?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  licenseInfo?: License;
  licenseCount: number;
}