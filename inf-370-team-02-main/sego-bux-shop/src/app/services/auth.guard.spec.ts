import { TestBed } from '@angular/core/testing';
import { AuthGuard } from './auth.guard';
import { RouterTestingModule } from '@angular/router/testing';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    const authSpy = jasmine.createSpyObj('AuthService', ['getToken', 'getRoleFromToken']);

    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authSpy }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should return true if token exists and role matches expected roles', () => {
    const mockRoute = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;

    authService.getToken.and.returnValue('valid-token');
    authService.getRoleFromToken.and.returnValue('Admin');

    const result = guard.canActivate(mockRoute);
    expect(result).toBeTrue();
  });

  it('should return true if token exists and no roles expected', () => {
    const mockRoute = { data: {} } as unknown as ActivatedRouteSnapshot;

    authService.getToken.and.returnValue('valid-token');
    authService.getRoleFromToken.and.returnValue('User');

    const result = guard.canActivate(mockRoute);
    expect(result).toBeTrue();
  });

  it('should redirect to /login and return false if no token', () => {
    const mockRoute = { data: {} } as unknown as ActivatedRouteSnapshot;
    authService.getToken.and.returnValue(null);

    const navigateSpy = spyOn(router, 'navigate');

    const result = guard.canActivate(mockRoute);
    expect(result).toBeFalse();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('should redirect to /unauthorized and return false if role is not allowed', () => {
    const mockRoute = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;

    authService.getToken.and.returnValue('valid-token');
    authService.getRoleFromToken.and.returnValue('User');

    const navigateSpy = spyOn(router, 'navigate');

    const result = guard.canActivate(mockRoute);
    expect(result).toBeFalse();
    expect(navigateSpy).toHaveBeenCalledWith(['/unauthorized']);
  });
});
