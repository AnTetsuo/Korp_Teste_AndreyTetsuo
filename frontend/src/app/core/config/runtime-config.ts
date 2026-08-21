import {
  EnvironmentProviders,
  Injectable,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';

import { API_BASE_URLS, ApiBaseUrls, RUNTIME_CONFIG_URL, parseApiBaseUrls } from './api-base-urls';

@Injectable({ providedIn: 'root' })
export class RuntimeConfig {
  private value: ApiBaseUrls | null = null;

  async load(): Promise<void> {
    let response: Response;

    try {
      response = await fetch(RUNTIME_CONFIG_URL, { cache: 'no-store' });
    } catch (cause) {
      throw new Error(`Could not reach ${RUNTIME_CONFIG_URL}.`, { cause });
    }

    if (!response.ok) {
      throw new Error(`Could not read ${RUNTIME_CONFIG_URL} (HTTP ${response.status}).`);
    }

    this.value = parseApiBaseUrls(await response.json());
  }

  get apiBaseUrls(): ApiBaseUrls {
    if (this.value === null) {
      throw new Error(
        `API_BASE_URLS was injected before ${RUNTIME_CONFIG_URL} finished loading. ` +
          'It is only available once the application initializer has run.',
      );
    }

    return this.value;
  }
}

export function provideRuntimeConfig(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => inject(RuntimeConfig).load()),
    { provide: API_BASE_URLS, useFactory: () => inject(RuntimeConfig).apiBaseUrls },
  ]);
}
