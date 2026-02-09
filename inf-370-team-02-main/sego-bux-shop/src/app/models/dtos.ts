export interface TimerPolicyDto {
  otpExpiryMinutes: number;
  sessionTimeoutMinutes: number;
  minOtpMinutes: number;
  maxOtpMinutes: number;
  minSessionMinutes: number;
  maxSessionMinutes: number;
  updatedAtUtc: string;
}

export interface CurrentTimerStateDto {
  nowUtc: string;
  otpExpiresAtUtc?: string | null;
  sessionExpiresAtUtc?: string | null;
}

// Audit list (align with your AuditLog entity)
export interface AuditLogDto {
  id: number;
  utcTimestamp: string;
  userEmailSnapshot?: string | null;
  action: string;
  entity: string;
  entityId?: string | null;
  criticalValue?: string | null;
  beforeJson?: string | null;
  afterJson?: string | null;
  ip?: string | null;
  userAgent?: string | null;
}

// Paged result wrapper
export interface PagedResult<T> {
  totalCount: number;
  items: T[];
}
