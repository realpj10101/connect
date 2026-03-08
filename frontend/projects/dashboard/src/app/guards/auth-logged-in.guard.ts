import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authLoggedInGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  const loggedInUserStr: string | null = localStorage.getItem('loggedInUser');

  if (loggedInUserStr) {
    router.navigate(['dashboard']);
    
    return false;
  }

  return true;
};
