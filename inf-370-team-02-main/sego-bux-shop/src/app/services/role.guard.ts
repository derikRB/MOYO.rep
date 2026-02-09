// src/app/services/role.guard.ts
import { CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { inject } from '@angular/core';

export const RoleGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const userRoles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    const requiredRoles: string[] = route.data['roles'];

    const userHasAccess = Array.isArray(userRoles)
      ? userRoles.some(role => requiredRoles.includes(role))
      : requiredRoles.includes(userRoles);

    if (!userHasAccess) {
      router.navigate(['/unauthorized']);
      return false;
    }

    return true;
  } catch {
    router.navigate(['/login']);
    return false;
  }
};
