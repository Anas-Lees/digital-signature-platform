import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { I18n } from '../../core/i18n.service';
import { BrandMark } from '../../shared/brand-mark';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, BrandMark],
  template: `
    <div class="auth-wrap">
      <div class="auth-head">
        <app-brand-mark [size]="46"></app-brand-mark>
        <h2>{{ i18n.t('auth.registerTitle') }}</h2>
        <p>{{ i18n.t('app.tagline') }}</p>
      </div>

      <div class="card">
        <label class="field">
          <span>{{ i18n.t('auth.displayName') }}</span>
          <input type="text" [(ngModel)]="displayName" name="displayName" autocomplete="name" />
        </label>
        <label class="field">
          <span>{{ i18n.t('auth.email') }}</span>
          <input type="email" [(ngModel)]="email" name="email" autocomplete="username" />
        </label>
        <label class="field">
          <span>{{ i18n.t('auth.password') }}</span>
          <input type="password" [(ngModel)]="password" name="password" autocomplete="new-password" />
        </label>
        @if (error()) { <p class="error">{{ error() }}</p> }
        <button class="btn primary block mt" [disabled]="busy()" (click)="submit()">
          {{ busy() ? i18n.t('common.loading') : i18n.t('auth.registerBtn') }}
        </button>
      </div>

      <p class="muted small center mt">
        {{ i18n.t('auth.haveAccount') }} <a routerLink="/login">{{ i18n.t('nav.login') }}</a>
      </p>
    </div>
  `
})
export class Register {
  private auth = inject(AuthService);
  private router = inject(Router);
  i18n = inject(I18n);

  displayName = '';
  email = '';
  password = '';
  busy = signal(false);
  error = signal('');

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.auth.register(this.email, this.password, this.displayName).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (e) => { this.error.set(e?.error?.message ?? 'Registration failed'); this.busy.set(false); }
    });
  }
}
