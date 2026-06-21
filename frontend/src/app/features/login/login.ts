import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { I18n } from '../../core/i18n.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="card auth-card">
      <h2>{{ i18n.t('auth.loginTitle') }}</h2>
      <label>{{ i18n.t('auth.email') }}
        <input type="email" [(ngModel)]="email" name="email" autocomplete="username" />
      </label>
      <label>{{ i18n.t('auth.password') }}
        <input type="password" [(ngModel)]="password" name="password" autocomplete="current-password" />
      </label>
      @if (error()) { <p class="error">{{ error() }}</p> }
      <button class="btn primary" [disabled]="busy()" (click)="submit()">
        {{ busy() ? i18n.t('common.loading') : i18n.t('auth.loginBtn') }}
      </button>
      <p class="muted small">{{ i18n.t('auth.demoHint') }}</p>
      <p class="muted">{{ i18n.t('auth.noAccount') }}
        <a routerLink="/register">{{ i18n.t('nav.register') }}</a></p>
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
