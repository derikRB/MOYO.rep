import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const token = this.auth.getToken();
    if (!token) {
      this.router.navigate(['/login']);
      return false;
    }

    const expectedRoles = route.data['roles'] as string[] | undefined;
    const userRole = this.auth.getRoleFromToken();

    if (expectedRoles && !expectedRoles.includes(userRole!)) {
      this.router.navigate(['/unauthorized']);
      return false;
    }

    return true;
  }
}
