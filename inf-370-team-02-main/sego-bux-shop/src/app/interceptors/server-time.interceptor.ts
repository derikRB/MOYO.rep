import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { TimeService } from '../services/time.service';

export const serverTimeInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const time = inject(TimeService);
  return next(req).pipe(
    tap({
      next: (evt: any) => {
        const headers = evt?.headers;
        if (headers?.get) {
          const dateHeader = headers.get('Date'); // standard UTC HTTP date
          if (dateHeader) time.setServerDateHeader(dateHeader);
        }
      }
    })
  );
};
