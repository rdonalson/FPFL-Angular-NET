/** AuthResponse from API-NET (/auth/login, /guest, /register, /refresh, /change-password). */
export interface AuthResponseDto {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  id: number;
  email: string;
  userId: string;
  first: string | null;
  last: string | null;
  roles: string[];
  isGuest: boolean;
}

/** The signed-in user's session as the UI keeps it. */
export interface Session {
  accessToken: string;
  /** Epoch milliseconds. */
  accessTokenExpiresAt: number;
  refreshToken: string;
  id: number;
  userId: string;
  email: string;
  first: string;
  last: string;
  roles: string[];
  isGuest: boolean;
}

export interface RegisterRequest {
  first: string;
  last: string;
  email: string;
  password: string;
}

export const ADMIN_ROLE = 'ROLE_ADMIN';

export function toSession(dto: AuthResponseDto): Session {
  return {
    accessToken: dto.accessToken,
    accessTokenExpiresAt: Date.parse(dto.accessTokenExpiresAt),
    refreshToken: dto.refreshToken,
    id: dto.id,
    userId: dto.userId,
    email: dto.email,
    first: dto.first ?? '',
    last: dto.last ?? '',
    roles: dto.roles ?? [],
    isGuest: dto.isGuest,
  };
}
