import { HttpErrorResponse } from '@angular/common/http';

export type ApiErrorKind =
  | 'network'
  | 'validation'
  | 'badRequest'
  | 'notFound'
  | 'conflict'
  | 'server'
  | 'unknown';

export const API_ERROR_MESSAGES: Readonly<Record<ApiErrorKind, string>> = {
  network: 'Não foi possível contactar o serviço. Verifique se ele está no ar.',
  validation: 'Verifique os campos destacados.',
  badRequest: 'Um parâmetro da requisição é inválido.',
  notFound: 'Registro não encontrado.',
  conflict: 'A operação conflita com o estado atual do registro.',
  server: 'Erro inesperado no servidor.',
  unknown: 'Não foi possível concluir a operação.',
};

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly kind: ApiErrorKind,
    message: string,
    readonly detail: string | null,
    readonly fieldErrors: Readonly<Record<string, readonly string[]>>,
    readonly traceId: string | null,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  get hasFieldErrors(): boolean {
    return Object.keys(this.fieldErrors).length > 0;
  }

  errorsFor(field: string): readonly string[] {
    return this.fieldErrors[field] ?? [];
  }
}

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return new ApiError(0, 'network', API_ERROR_MESSAGES.network, null, {}, null);
  }

  const problem = asRecord(response.error);
  const fieldErrors = readFieldErrors(problem);
  const detail = readText(problem, 'detail');
  const kind = classify(response.status, fieldErrors);

  return new ApiError(
    response.status,
    kind,
    messageFor(kind, detail),
    detail,
    fieldErrors,
    readText(problem, 'traceId'),
  );
}

function classify(
  status: number,
  fieldErrors: Readonly<Record<string, readonly string[]>>,
): ApiErrorKind {
  if (status === 400) {
    return Object.keys(fieldErrors).length > 0 ? 'validation' : 'badRequest';
  }

  if (status === 404) {
    return 'notFound';
  }

  if (status === 409) {
    return Object.keys(fieldErrors).length > 0 ? 'validation' : 'conflict';
  }

  return status >= 500 ? 'server' : 'unknown';
}

function messageFor(kind: ApiErrorKind, detail: string | null): string {
  if (kind === 'notFound' || kind === 'conflict') {
    return detail ?? API_ERROR_MESSAGES[kind];
  }

  return API_ERROR_MESSAGES[kind];
}

function asRecord(body: unknown): Record<string, unknown> | null {
  let candidate = body;

  if (typeof candidate === 'string') {
    try {
      candidate = JSON.parse(candidate);
    } catch {
      return null;
    }
  }

  return candidate !== null && typeof candidate === 'object' && !Array.isArray(candidate)
    ? (candidate as Record<string, unknown>)
    : null;
}

function readText(problem: Record<string, unknown> | null, key: string): string | null {
  const value = problem?.[key];

  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function readFieldErrors(
  problem: Record<string, unknown> | null,
): Readonly<Record<string, readonly string[]>> {
  const raw = problem?.['errors'];

  if (raw === null || typeof raw !== 'object' || Array.isArray(raw)) {
    return {};
  }

  const collected: Record<string, readonly string[]> = {};

  for (const [field, value] of Object.entries(raw as Record<string, unknown>)) {
    const messages = Array.isArray(value)
      ? value.filter((entry): entry is string => typeof entry === 'string')
      : typeof value === 'string'
        ? [value]
        : [];

    if (messages.length > 0) {
      collected[field] = messages;
    }
  }

  return collected;
}

export function describeForSupport(error: ApiError): string {
  const traceable = error.kind === 'server' || error.kind === 'unknown';

  return traceable && error.traceId !== null
    ? `${error.message} (trace ${error.traceId})`
    : error.message;
}
