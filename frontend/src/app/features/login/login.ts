import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { I18n } from '../../core/i18n.service';
import { BrandMark } from '../../shared/brand-mark';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, BrandMark],
  template: `
    <div class="auth-wrap">
      <div class="auth-head">
        <app-brand-mark [size]="46"></app-brand-mark>
        <h2>{{ i18n.t('auth.loginTitle') }}</h2>
        <p>{{ i18n.t('app.tagline') }}</p>
      </div>

      <div class="card">
        <label class="field">
          <span>{{ i18n.t('auth.email') }}</span>
          <input type="email" [(ngModel)]="email" name="email" autocomplete="username" />
        </label>
        <label class="field">
          <span>{{ i18n.t('auth.password') }}</span>
          <input type="password" [(ngModel)]="password" name="password" autocomplete="current-password" />
        </label>
        @if (error()) { <p class="error">{{ error() }}</p> }
        <button class="btn primary block mt" [disabled]="busy()" (click)="submit()">
          {{ busy() ? i18n.t('common.loading') : i18n.t('auth.loginBtn') }}
        </button>
        <p class="muted small center mt">{{ i18n.t('auth.demoHint') }}</p>
      </div>

      <p class="muted small center mt">
        {{ i18n.t('auth.noAccount') }} <a routerLink="/register">{{ i18n.t('nav.register') }}</a>
      </p>
    </div>
  `
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);
  i18n = inject(I18n);

  email = 'demo@signvault.local';
  password = 'Demo1234!';
  busy = signal(false);
  error = signal('');

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e) => { this.error.set(e?.error?.message ?? 'Login failed'); this.busy.set(false); }
    });
  }
}
