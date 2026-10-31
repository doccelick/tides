import type { CreateImportRequest, ImportsResponse } from "@/api/generated";
import { sendJson } from "@/api/sendJson";
import { getAdminToken } from "@/auth/getAdminToken";
import type { UtcRange } from "./importPresets";

export async function createImport(locationId: number, range: UtcRange) {
  const request: CreateImportRequest = {
    from: `${range.from}T00:00:00Z`,
    to: `${range.to}T00:00:00Z`,
  };

  return sendJson<ImportsResponse>(
    "POST",
    `/v1/locations/${locationId}/imports`,
    request,
    await getAdminToken(),
  );
}

export async function deleteImport(importId: number) {
  return sendJson<undefined>("DELETE", `/v1/imports/${importId}`, undefined, await getAdminToken());
}
