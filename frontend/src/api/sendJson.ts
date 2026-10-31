import type { HttpValidationProblemDetails } from "@/api/generated";

export interface ApiError {
  message: string;
  // Validation messages by field name, from a 400 response.
  fieldErrors: Record<string, string[]>;
}

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: ApiError };

const unreachable: ApiError = { message: "Couldn't reach the API", fieldErrors: {} };

// Sends a request with an optional JSON body. An error response becomes an ApiError whose
// message is the ProblemDetails title, so the API decides the wording.
export async function sendJson<T>(
  method: string,
  url: string,
  body?: unknown,
): Promise<ApiResult<T>> {
  const init: RequestInit =
    body === undefined
      ? { method }
      : { method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) };

  let response: Response;
  try {
    response = await fetch(url, init);
  } catch {
    return { ok: false, error: unreachable };
  }

  if (!response.ok) {
    return { ok: false, error: await readProblem(response) };
  }

  if (response.status === 204) {
    return { ok: true, data: undefined as T };
  }

  const data: T = await response.json();
  return { ok: true, data };
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
