// Payment Types
export interface Payment {
  id: string;
  userId: string;
  amount: number;
  currency: string;
  status: 'pending' | 'succeeded' | 'failed' | 'refunded';
  paymentMethod?: string;
  stripePaymentIntentId?: string;
  licenseType: string;
  description: string;
  user: {
    email: string;
    firstName: string;
    lastName: string;
  };
  createdAt: string;
  updatedAt: string;
}