export interface RegisterRequestDto {
  email: string;
  username: string;
  password: string;
  displayName?: string;
  role: 'Reader' | 'Blogger' | 'Admin';
}

export interface LoginRequestDto {
  emailOrUsername: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
}

export interface MeResponseDto {
  id: string;
  username: string;
  displayName?: string;
  email: string;
  role: string;
  avatarUrl?: string;   // ← added so navbar avatar image works
}

export interface ForgotPasswordRequestDto {
  email: string;
}

export interface ResetPasswordRequestDto {
  token: string;
  newPassword: string;
}
