import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  const loggedInUserStr: string | null = localStorage.getItem('loggedInUser');

  if (loggedInUserStr) {
    return true;
  }

  localStorage.setItem('returnUrl', state.url);

  router.navigate(['/sign-in'], { queryParams: { 'returnUrl': state.url } });

  return false;
};
