import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { provideRuntimeConfig } from './core/config/runtime-config';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { KorpPaginatorIntl } from './shared/paginator-intl';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([problemDetailsInterceptor])),
    provideRuntimeConfig(),
    { provide: MatPaginatorIntl, useClass: KorpPaginatorIntl },
  ],
};
