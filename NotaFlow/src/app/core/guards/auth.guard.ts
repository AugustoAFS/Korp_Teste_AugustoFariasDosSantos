import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.authenticated() ? true : router.createUrlTree(['/entrar']);
};

export const managerGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.authenticated()) return router.createUrlTree(['/entrar']);

  return auth.manager() ? true : router.createUrlTree(['/sem-acesso']);
};
