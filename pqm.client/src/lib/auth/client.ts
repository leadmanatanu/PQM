'use client';

import axios from 'axios';
import type { User } from '@/types/user';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5135/api';

export interface SignUpParams {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface SignInWithOAuthParams {
  provider: 'google' | 'discord';
}

export interface SignInWithPasswordParams {
  email: string;
  password: string;
}

export interface ResetPasswordParams {
  email: string;
}

interface ApiResponse<T = any> {
  status: boolean;
  statusCode: number;
  data: T;
  errors: string[];
}

class AuthClient {
  async signUp(params: SignUpParams): Promise<{ error?: string }> {
    try {
      const response = await axios.post<ApiResponse>(`${API_URL}/auth/signup`, {
        firstName: params.firstName,
        lastName: params.lastName,
        email: params.email,
        password: params.password
      });

      const res = response.data;
      if (res.status && res.data?.token) {
        localStorage.setItem('custom-auth-token', res.data.token);
        // Store user info in localStorage for client retrieval
        localStorage.setItem('pqm_current_user', JSON.stringify(res.data.user));
        return {};
      } else {
        return { error: res.errors?.[0] || 'Failed to sign up.' };
      }
    } catch (error: any) {
      console.error('Sign up error:', error);
      return { error: error.response?.data?.errors?.[0] || error.message || 'Server error during sign up.' };
    }
  }

  async signInWithOAuth(_: SignInWithOAuthParams): Promise<{ error?: string }> {
    return { error: 'Social authentication not implemented' };
  }

  async signInWithPassword(params: SignInWithPasswordParams): Promise<{ error?: string }> {
    const { email, password } = params;

    try {
      const response = await axios.post<ApiResponse>(`${API_URL}/auth/login`, {
        email,
        password
      });

      const res = response.data;
      if (res.status && res.data?.token) {
        localStorage.setItem('custom-auth-token', res.data.token);
        // Store user info in localStorage for client retrieval
        localStorage.setItem('pqm_current_user', JSON.stringify(res.data.user));
        return {};
      } else {
        return { error: res.errors?.[0] || 'Invalid credentials' };
      }
    } catch (error: any) {
      console.error('Login error:', error);
      return { error: error.response?.data?.errors?.[0] || error.message || 'Server error during login.' };
    }
  }

  async resetPassword(_: ResetPasswordParams): Promise<{ error?: string }> {
    return { error: 'Password reset not implemented' };
  }

  async updatePassword(_: ResetPasswordParams): Promise<{ error?: string }> {
    return { error: 'Update reset not implemented' };
  }

  async getUser(): Promise<{ data?: User | null; error?: string }> {
    const token = localStorage.getItem('custom-auth-token');
    const userStr = localStorage.getItem('pqm_current_user');

    if (!token || !userStr) {
      return { data: null };
    }

    try {
      const user = JSON.parse(userStr) as User;
      return { data: user };
    } catch (error) {
      console.error('Error parsing stored user:', error);
      localStorage.removeItem('custom-auth-token');
      localStorage.removeItem('pqm_current_user');
      return { data: null };
    }
  }

  async signOut(): Promise<{ error?: string }> {
    const token = localStorage.getItem('custom-auth-token');
    
    if (token) {
      try {
        await axios.post(`${API_URL}/auth/logout`, null, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });
      } catch (error) {
        console.error('Sign out error on server:', error);
      }
    }

    localStorage.removeItem('custom-auth-token');
    localStorage.removeItem('pqm_current_user');
    return {};
  }
}

export const authClient = new AuthClient();
