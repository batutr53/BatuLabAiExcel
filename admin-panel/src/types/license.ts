// License Management Types  
export interface License {
  id: string;
  licenseKey: string;
  type: number;
  status: number;
  isActive: boolean;
  startDate: string;
  expiresAt?: string;
  createdAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
  };
}