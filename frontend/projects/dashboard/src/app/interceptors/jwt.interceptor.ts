import { HttpInterceptorFn } from '@angular/common/http';
import { LoggedInUser } from '../models/logged-in-user.model';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const loggedInUserStr: string | null = localStorage.getItem('loggedInUser');

  if (loggedInUserStr) {
    const loggedInUser: LoggedInUser = JSON.parse(loggedInUserStr);

    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${loggedInUser.token}`
      }
    })
  }

  return next(req);
};
