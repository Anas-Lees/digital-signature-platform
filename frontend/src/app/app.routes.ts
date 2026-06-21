import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { Documents } from './features/documents/documents';
import { Login } from './features/login/login';
import { Register } from './features/register/register';
import { Verify } from './features/verify/verify';

export const routes: Routes = [
  { path: '', component: Documents, canActivate: [authGuard] },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'verify', component: Verify },
  { path: '**', redirectTo: '' }
];
