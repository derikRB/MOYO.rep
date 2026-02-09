import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/services/auth.interceptor';
import { serverTimeInterceptor } from './app/interceptors/server-time.interceptor';

import { environment } from './environments/environment';
import emailjs from '@emailjs/browser';

emailjs.init(environment.emailJsUserId);

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor, serverTimeInterceptor])),
    provideRouter(routes),
  ]
}).catch(err => console.error(err));
