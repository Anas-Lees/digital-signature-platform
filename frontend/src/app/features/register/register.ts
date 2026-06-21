import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { I18n } from '../../core/i18n.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="card auth-card">
      <h2>{{ i18n.t('auth.registerTitle') }}</h2>
      <label>{{ i18n.t('auth.displayName') }}
        <input type="text" [(ngModel)]="displayName" name="displayName" autocomplete="name" />
      </label>
      <label>{{ i18n.t('auth.email') }}
        <input type="email" [(ngModel)]="email" name="email" autocomplete="username" />
      </label>
      <label>{{ i18n.t('auth.password') }}
        <input type="password" [(ngModel)]="password" name="password" autocomplete="new-password" />
      </label>
      @if (error()) { <p class="error">{{ error() }}</p> }
      <button class="btn primary" [disabled]="busy()" (click)="submit()">
        {{ busy() ? i18n.t('common.loading') : i18n.t('auth.registerBtn') }}
      </button>
      <p class="muted">{{ i18n.t('auth.haveAccount') }}
        <a routerLink="/login">{{ i18n.t('nav.login') }}</a></p>
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
