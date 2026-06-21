import { Component, inject } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/auth.service';
import { I18n } from './core/i18n.service';
import { BrandMark } from './shared/brand-mark';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, BrandMark],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  auth = inject(AuthService);
  i18n = inject(I18n);
  private router = inject(Router);

  logout(): void { this.auth.logout(); this.router.navigateByUrl('/login'); }
  toggleLang(): void { this.i18n.toggle(); }
}
