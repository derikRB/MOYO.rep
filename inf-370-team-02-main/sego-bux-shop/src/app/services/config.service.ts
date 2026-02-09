import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface TimerPolicyDto {
  otpExpiryMinutes: number;
  sessionTimeoutMinutes: number;
  minOtpMinutes: number;
  maxOtpMinutes: number;
  minSessionMinutes: number;
  maxSessionMinutes: number;
  updatedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private base = `${environment.apiUrl}/api/config`;
  constructor(private http: HttpClient) {}

  getTimers() {
    return this.http.get<TimerPolicyDto>(`${this.base}/timers`);
  }

  saveTimers(dto: Pick<TimerPolicyDto, 'otpExpiryMinutes' | 'sessionTimeoutMinutes'>) {
    return this.http.put<void>(`${this.base}/timers`, dto);
  }

  // public endpoint used by verify-otp page
  getOtpMinutes() {
    return this.http.get<number>(`${this.base}/otp-minutes`);
  }
}
