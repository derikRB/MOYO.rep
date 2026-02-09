import { Routes } from '@angular/router';
import { AuthGuard } from './services/auth.guard';
import { RoleGuard } from './services/role.guard';
import { AuthShellComponent } from './auth/auth-shell/auth-shell.component';
import { PayProductComponent } from './pages/pay-product.component';

export const routes: Routes = [
  // --- Public ---
  { path: '', loadComponent: () => import('./pages/landing/landing.component').then(m => m.LandingComponent) },
  { path: 'products', loadComponent: () => import('./shop/product-list.component').then(m => m.ProductListComponent) },
  { path: 'products/:id', loadComponent: () => import('./shop/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },
  { path: 'contact', loadComponent: () => import('./contact/contact.component').then(m => m.ContactComponent) },

  // --- Auth ---
  {
    path: 'auth',
    component: AuthShellComponent,
    children: [
      { path: 'login',           loadComponent: () => import('./auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register',        loadComponent: () => import('./auth/register/register.component').then(m => m.RegisterComponent) },
      { path: 'verify-otp',      loadComponent: () => import('./auth/verify-otp/verify-otp.component').then(m => m.VerifyOtpComponent) },
      { path: 'forgot-password', loadComponent: () => import('./auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
      { path: 'reset-password',  loadComponent: () => import('./auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },
  { path: 'login',    redirectTo: 'auth/login',    pathMatch: 'full' },
  { path: 'register', redirectTo: 'auth/register', pathMatch: 'full' },

  // --- Customer (Logged In) ---
  { path: 'account',        loadComponent: () => import('./account/account.component').then(m => m.AccountComponent), canActivate: [AuthGuard] },
  { path: 'profile',        loadComponent: () => import('./profile/customer-profile/customer-profile.component').then(m => m.CustomerProfileComponent), canActivate: [AuthGuard] },
  { path: 'profile/orders', loadComponent: () => import('./profile/customer-orders.component').then(m => m.CustomerOrdersComponent), canActivate: [AuthGuard] },
  { path: 'cart',           loadComponent: () => import('./cart/cart/cart.component').then(m => m.CartComponent), canActivate: [AuthGuard] },
  { path: 'checkout',       loadComponent: () => import('./cart/checkout/checkout.component').then(m => m.CheckoutComponent), canActivate: [AuthGuard] },
  { path: 'pay/:id',        component: PayProductComponent },

  // --- Feedback & Product Review ---
  {
    path: 'feedback/:orderId',
    loadComponent: () => import('./feedback/feedback-form/feedback-form.component').then(m => m.FeedbackFormComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'product-review/:orderId',
    loadComponent: () => import('./shop/product-review-form/product-review-form.component').then(m => m.ProductReviewFormComponent),
    canActivate: [AuthGuard]
  },

  // --- Admin / Employee / Manager ---
  {
    path: 'admin',
    loadComponent: () => import('./admin/admin-dashboard/admin-dashboard.component').then(m => m.DashboardComponent),
    canActivate: [RoleGuard],
    data: { roles: ['Employee','Manager','Admin'] },
    children: [
      { path: '',       redirectTo: 'system', pathMatch: 'full' },
      // NOTE: path now matches your actual folder: /src/app/system-dashboard/...
      { path: 'system', loadComponent: () => import('./system-dashboard/system-dashboard.component').then(m => m.SystemDashboardComponent) },

      { path: 'reports',         loadComponent: () => import('./admin-reports/admin-reports.component').then(m => m.AdminReportsComponent) },
      { path: 'products',        loadComponent: () => import('./admin/manage-products.component').then(m => m.ManageProductsComponent) },
      { path: 'product-types',   loadComponent: () => import('./admin/manage-product-types.component').then(m => m.ManageProductTypesComponent) },
      { path: 'categories',      loadComponent: () => import('./admin/manage-categories.component').then(m => m.ManageCategoriesComponent) },
      { path: 'employees',       loadComponent: () => import('./admin/manage-employees/manage-employees.component').then(m => m.ManageEmployeesComponent) },
      { path: 'templates',       loadComponent: () => import('./admin/manage-templates/manage-templates.component').then(m => m.ManageTemplatesComponent) },
      { path: 'vat',             loadComponent: () => import('./admin/manage-vat/manage-vat/manage-vat.component').then(m => m.ManageVatComponent) },
      { path: 'stock-purchases', loadComponent: () => import('./admin/stock-purchase/stock-purchase.component').then(m => m.StockPurchaseComponent) },
      { path: 'stock-receipts',  loadComponent: () => import('./admin/stock-receipts/receive-stock/receive-stock.component').then(m => m.ReceiveStockComponent) },
      { path: 'stock-adjustments', loadComponent: () => import('./admin/stock-adjustments/stock-adjustment/stock-adjustment.component').then(m => m.StockAdjustmentComponent) },
      { path: 'low-stock-alerts',   loadComponent: () => import('./admin/low-stock-alerts/low-stock-alerts.component').then(m => m.LowStockAlertsComponent) },
      { path: 'faqs',               loadComponent: () => import('./admin/faq-manager/faq-manager.component').then(m => m.FaqManagerComponent) },
      { path: 'chatbot-config',     loadComponent: () => import('./admin-chatbot-config/admin-chatbot-config.component').then(m => m.AdminChatbotConfigComponent) },
      { path: 'orders',             loadComponent: () => import('./admin-manage-orders/admin-manage-orders.component').then(m => m.AdminManageOrdersComponent) },
      { path: 'product-reviews',    loadComponent: () => import('./admin/manage-product-reviews/manage-product-reviews.component').then(m => m.ManageProductReviewsComponent) },
      { path: 'feedback',        loadComponent: () => import('./admin/manage-feedbacks/manage-feedbacks.component').then(m => m.ManageFeedbacksComponent) }

    ]
  },

  // --- Fallback ---
  { path: '**', redirectTo: '' }
];
