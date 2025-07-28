// License Management Types
export interface License {
  id: string;
  userId: string;
  type: 'trial' | 'monthly' | 'yearly' | 'lifetime';
  status: 'active' | 'expired' | 'suspended';
  startDate: string;
  expiryDate?: string;
  createdAt: string;
  features?: string[];
}