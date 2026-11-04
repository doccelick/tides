import type { ImportsResponse } from "@/api/generated";
import { sendJson } from "@/api/sendJson";
import { getAdminToken } from "@/auth/getAdminToken";

// Imports the months of the location's window that are not imported yet.
export async function createImport(locationId: number) {
  return sendJson<ImportsResponse>(
    "POST",
    `/v1/locations/${locationId}/imports`,
    undefined,
    await getAdminToken(),
  );
}

export async function deleteImport(importId: number) {
  return sendJson<undefined>("DELETE", `/v1/imports/${importId}`, undefined, await getAdminToken());
}
