import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE } from './api.config';
import { AuthResponse, UserDto } from './models';

const TOKEN_KEY = 'sv_token';
const USER_KEY = 'sv_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly user = signal<UserDto | null>(this.readUser());
  readonly isLoggedIn = computed(() => !!this.token());

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE}/auth/login`, { email, password })
      .pipe(tap(res => this.persist(res)));
  }

  register(email: string, password: string, displayName: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE}/auth/register`, { email, password, displayName })
      .pipe(tap(res => this.persist(res)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.user.set(null);
  }

  private persist(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.token.set(res.token);
    this.user.set(res.user);
  }

  private readUser(): UserDto | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) as UserDto : null;
  }
}
