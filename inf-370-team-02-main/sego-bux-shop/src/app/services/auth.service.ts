import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, catchError, tap } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/Auth`;

  private loggedIn = new BehaviorSubject<boolean>(!!this.getToken());
  public isLoggedIn$ = this.loggedIn.asObservable();

  private roleSubject = new BehaviorSubject<string | null>(this.getRoleFromToken());
  public role$ = this.roleSubject.asObservable();

  constructor(private http: HttpClient) {}

  loginAuto(data: { emailOrUsername: string; password: string }) {
    return this.http
      .post<{ token: string; refreshToken: string }>(
        `${this.apiUrl}/employee/login`, data
      ).pipe(
        tap(resp => this.handleLoginSuccess(resp)),
        catchError(() =>
          this.http
            .post<{ token: string; refreshToken: string }>(
              `${this.apiUrl}/customer/login`, data
            )
            .pipe(tap(resp => this.handleLoginSuccess(resp)))
        )
      );
  }

  register(data: any) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/customer/register`, data
    );
  }

  verifyOtp(payload: { email: string; otp: string }): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/customer/verify-otp`, payload)
      .pipe(
        tap((res: any) => {
          if (res.token && res.refreshToken) {
            this.handleLoginSuccess(res);
          }
        })
      );
  }

  resendOtp(payload: { email: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/customer/resend-otp`, payload);
  }

  private handleLoginSuccess(resp: { token: string; refreshToken: string }) {
    this.saveToken(resp.token);
    this.loggedIn.next(true);

    const role = this.getRoleFromToken();
    this.roleSubject.next(role);
    if (role) localStorage.setItem('userRole', role);

    const userId = this.getUserId();
    if (userId !== null) localStorage.setItem('userId', userId.toString());
  }

  saveToken(token: string) {
    localStorage.setItem('token', token);
    const role = this.getRoleFromToken();
    if (role) localStorage.setItem('userRole', role);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('userRole');
    localStorage.removeItem('userId');
    this.loggedIn.next(false);
    this.roleSubject.next(null);
  }

  getRoleFromToken(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const rc = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return Array.isArray(rc) ? rc[0] : rc;
    } catch {
      return null;
    }
  }

  public getUserId(): number | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const idClaim = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      return idClaim ? Number(idClaim) : null;
    } catch {
      return null;
    }
  }

  forgotPassword(email: string) {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(payload: {
    email: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }) {
    return this.http.post<any>(`${this.apiUrl}/reset-password`, payload);
  }

  validateResetToken(email: string, token: string) {
    return this.http.get(`${this.apiUrl}/validate-reset-token`, {
      params: { email, token }
    });
  }

  /** Staff-only friendly name for UI (Employee/Admin/Manager) */
  getUserName(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const allowedRoles = ['Employee', 'Admin', 'Manager'];
      const userRole = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const staffRole = Array.isArray(userRole) ? userRole[0] : userRole;
      if (!allowedRoles.includes(staffRole)) return null;
      return (
        payload['username'] ||
        payload['email'] ||
        payload['name'] ||
        'Employee User'
      );
    } catch {
      return null;
    }
  }

  /** 🔑 New: extract staff identity from token */
  getStaffIdentity(): { id: number | null; username: string | null; role: string | null } {
    const token = this.getToken();
    if (!token) return { id: null, username: null, role: null };
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const idClaim = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      const roleRaw = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const role = Array.isArray(roleRaw) ? roleRaw[0] : roleRaw;
      const username = payload['username'] || payload['email'] || null;
      return { id: idClaim ? Number(idClaim) : null, username, role };
    } catch {
      return { id: null, username: null, role: null };
    }
  }

  /** 🔑 New: pretty label for chips */
  getStaffDisplay(): string {
    const s = this.getStaffIdentity();
    if (!s.id && !s.username) return 'Unknown User';
    return s.username && s.role ? `${s.username} (${s.role})`
         : s.username ? s.username
         : `User ${s.id}`;
  }
}
