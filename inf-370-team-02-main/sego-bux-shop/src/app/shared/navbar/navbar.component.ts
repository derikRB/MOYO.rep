import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { ToastService } from '../../shared/toast.service'; // <-- added for logout toaster

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  dropdownOpen = false;
  mobileMenuOpen = false;
  isLoggedIn = false;
  isEmployeeOrAdmin = false;
  cartCount = 0;
  role = '';
  isCustomer = false;

  constructor(
    public authService: AuthService,
    private cartService: CartService,
    private router: Router,
    private toastSvc: ToastService   // <-- added
  ) {}

  ngOnInit() {
    this.authService.isLoggedIn$.subscribe(status => {
      this.isLoggedIn = status;
      this.role = this.authService.getRoleFromToken() || '';
      this.isEmployeeOrAdmin = ['Employee', 'Admin', 'Manager'].includes(this.role);
      this.isCustomer = this.role === 'Customer';
      this.updateCartCount();
    });

    this.cartService.cart$.subscribe(() => this.updateCartCount());
    this.updateCartCount();
  }

  updateCartCount() {
    this.cartCount = this.cartService.getItems().reduce((sum, item) => sum + item.quantity, 0);
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  closeDropdown() {
    this.dropdownOpen = false;
  }

  toggleMobileMenu() {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    this.closeDropdown();
  }

  closeMobileMenu() {
    this.mobileMenuOpen = false;
  }

  // --- NAVIGATION METHODS ---
  navigateToCart() {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/cart']);
    }
    this.closeDropdown();
    this.closeMobileMenu();
  }

  navigateToProfile() {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/profile']);
    }
    this.closeDropdown();
    this.closeMobileMenu();
  }

  navigateToOrderHistory() {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
    } else {
      this.router.navigate(['/profile/orders']);
    }
    this.closeDropdown();
    this.closeMobileMenu();
  }

  logout() {
    this.authService.logout();
    this.closeDropdown();
    this.closeMobileMenu();
    this.router.navigate(['/login']);
    this.toastSvc.show('You have been logged out successfully.');  // <-- applied logout toaster
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    const target = event.target as HTMLElement;
    if (!target.closest('.account-dropdown') && !target.closest('.drop-toggle')) {
      this.closeDropdown();
    }
    if (!target.closest('.mobile-menu') && !target.closest('.hamburger')) {
      this.closeMobileMenu();
    }
  }
}
