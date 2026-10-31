import type { HttpValidationProblemDetails } from "@/api/generated";

export interface ApiError {
  message: string;
  // Validation messages by field name, from a 400 response.
  fieldErrors: Record<string, string[]>;
}

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: ApiError };

const unreachable: ApiError = { message: "Couldn't reach the API", fieldErrors: {} };
// The API's 401 title is only "Unauthorized", which doesn't say what to do.
const signInAgain: ApiError = { message: "Sign in again to save changes", fieldErrors: {} };

// Sends a request with an optional JSON body and bearer token. An error response becomes an
// ApiError whose message is the ProblemDetails title, so the API decides the wording.
export async function sendJson<T>(
  method: string,
  url: string,
  body?: unknown,
  token?: string,
): Promise<ApiResult<T>> {
  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token !== undefined) headers.Authorization = `Bearer ${token}`;

  const init: RequestInit =
    body === undefined ? { method, headers } : { method, headers, body: JSON.stringify(body) };

  let response: Response;
  try {
    response = await fetch(url, init);
  } catch {
    return { ok: false, error: unreachable };
  }

  if (response.status === 401) {
    return { ok: false, error: signInAgain };
  }

  if (!response.ok) {
    return { ok: false, error: await readProblem(response) };
  }

  if (response.status === 204) {
    return { ok: true, data: undefined as T };
  }

  try {
    const data: T = await response.json();
    return { ok: true, data };
  } catch {
    const message = `Unexpected response from the API (${response.status})`;
    return { ok: false, error: { message, fieldErrors: {} } };
  }
}

async function readProblem(response: Response): Promise<ApiError> {
  const fallback = `Request failed with ${response.status}`;

  try {
    const problem: HttpValidationProblemDetails = await response.json();
    return { message: problem.title ?? fallback, fieldErrors: problem.errors ?? {} };
  } catch {
    return { message: fallback, fieldErrors: {} };
  }
}
