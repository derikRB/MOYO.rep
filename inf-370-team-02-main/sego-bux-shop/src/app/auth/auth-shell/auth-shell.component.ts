// src/app/auth/auth-shell/auth-shell.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterModule }      from '@angular/router';
import { CommonModule }              from '@angular/common';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './auth-shell.component.html',
  styleUrls: ['./auth-shell.component.scss']
})
export class AuthShellComponent implements OnInit {
  mode: 'login' | 'register' = 'login';
  private router = inject(Router);

  ngOnInit() {
    // decide initial panel based on URL
    this.mode = this.router.url.includes('/register') ? 'register' : 'login';
  }

  showLogin() {
    this.mode = 'login';
    this.router.navigate(['/auth/login']);
  }

  showRegister() {
    this.mode = 'register';
    this.router.navigate(['/auth/register']);
  }
}
