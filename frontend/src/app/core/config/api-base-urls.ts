import { InjectionToken } from '@angular/core';

export interface ApiBaseUrls {
  readonly stock: string;
  readonly invoicing: string;
}

export const API_BASE_URLS = new InjectionToken<ApiBaseUrls>('API_BASE_URLS');

export const RUNTIME_CONFIG_URL = '/config.json';

export function parseApiBaseUrls(raw: unknown): ApiBaseUrls {
  if (raw === null || typeof raw !== 'object' || Array.isArray(raw)) {
    throw new Error(`${RUNTIME_CONFIG_URL} must contain a JSON object.`);
  }

  const source = raw as Record<string, unknown>;

  return {
    stock: readBaseUrl(source, 'stock'),
    invoicing: readBaseUrl(source, 'invoicing'),
  };
}

function readBaseUrl(source: Record<string, unknown>, key: string): string {
  const value = source[key];

  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new Error(`${RUNTIME_CONFIG_URL} is missing a "${key}" base URL.`);
  }

  const trimmed = value.trim();
  let parsed: URL;

  try {
    parsed = new URL(trimmed);
  } catch {
    throw new Error(
      `${RUNTIME_CONFIG_URL} has an invalid "${key}" base URL: "${trimmed}". ` +
        'It must be absolute and reachable from the browser, for example http://localhost:3000.',
    );
  }

  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw new Error(
      `${RUNTIME_CONFIG_URL} has a "${key}" base URL that is not http or https: "${trimmed}".`,
    );
  }

  return trimmed.replace(/\/+$/, '');
}
