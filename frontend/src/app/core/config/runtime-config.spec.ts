import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URLS, RUNTIME_CONFIG_URL, parseApiBaseUrls } from './api-base-urls';
import { RuntimeConfig, provideRuntimeConfig } from './runtime-config';

describe('parseApiBaseUrls', () => {
  it('reads both base URLs', () => {
    const urls = parseApiBaseUrls({
      stock: 'http://localhost:3000',
      invoicing: 'http://localhost:3001',
    });

    expect(urls).toEqual({
      stock: 'http://localhost:3000',
      invoicing: 'http://localhost:3001',
    });
  });

  it('strips trailing slashes so a path can be appended directly', () => {
    const urls = parseApiBaseUrls({
      stock: 'http://localhost:3000/',
      invoicing: 'https://invoicing.example.com///',
    });

    expect(urls.stock).toBe('http://localhost:3000');
    expect(urls.invoicing).toBe('https://invoicing.example.com');
  });

  it.each([
    ['a missing key', { stock: 'http://localhost:3000' }],
    ['an empty value', { stock: '  ', invoicing: 'http://localhost:3001' }],
    ['a non-string value', { stock: 3000, invoicing: 'http://localhost:3001' }],
    ['a relative URL', { stock: '/api', invoicing: 'http://localhost:3001' }],
    ['a non-http protocol', { stock: 'ftp://localhost:3000', invoicing: 'http://localhost:3001' }],
  ])('rejects %s', (_case, raw) => {
    expect(() => parseApiBaseUrls(raw)).toThrowError(/config\.json/);
  });

  it.each([
    ['null', null],
    ['a string', 'http://localhost:3000'],
    ['an array', []],
  ])('rejects %s at the top level', (_case, raw) => {
    expect(() => parseApiBaseUrls(raw)).toThrowError(/must contain a JSON object/);
  });
});

describe('RuntimeConfig', () => {
  function respondWith(body: unknown, ok = true, status = 200): void {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ ok, status, json: async () => body }) as Response),
    );
  }

  afterEach(() => vi.unstubAllGlobals());

  it('exposes the base URLs after load', async () => {
    respondWith({ stock: 'http://localhost:3000', invoicing: 'http://localhost:3001' });
    const config = TestBed.inject(RuntimeConfig);

    await config.load();

    expect(config.apiBaseUrls.stock).toBe('http://localhost:3000');
    expect(config.apiBaseUrls.invoicing).toBe('http://localhost:3001');
    expect(fetch).toHaveBeenCalledWith(RUNTIME_CONFIG_URL, { cache: 'no-store' });
  });

  it('throws when read before load', () => {
    const config = TestBed.inject(RuntimeConfig);

    expect(() => config.apiBaseUrls).toThrowError(/before .*config\.json finished loading/);
  });

  it('throws when the file is missing', async () => {
    respondWith({}, false, 404);
    const config = TestBed.inject(RuntimeConfig);

    await expect(config.load()).rejects.toThrowError(/HTTP 404/);
  });

  it('throws when the file cannot be reached', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw new TypeError('network down');
      }),
    );
    const config = TestBed.inject(RuntimeConfig);

    await expect(config.load()).rejects.toThrowError(/Could not reach/);
  });
});

describe('provideRuntimeConfig', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('fills API_BASE_URLS from the file before the application starts', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          stock: 'http://stock.test:3000/',
          invoicing: 'http://invoicing.test:3001',
        }),
      }) as Response),
    );

    TestBed.configureTestingModule({ providers: [provideRuntimeConfig()] });
    await TestBed.inject(ApplicationInitStatus).donePromise;

    expect(TestBed.inject(API_BASE_URLS)).toEqual({
      stock: 'http://stock.test:3000',
      invoicing: 'http://invoicing.test:3001',
    });
  });
});
