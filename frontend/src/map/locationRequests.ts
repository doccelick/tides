import type { CreateLocationRequest, LocationResponse } from "@/api/generated";
import { sendJson } from "@/api/sendJson";
import { getAdminToken } from "@/auth/getAdminToken";

export async function createLocation(request: CreateLocationRequest) {
  return sendJson<LocationResponse>("POST", "/v1/locations", request, await getAdminToken());
}

export async function deleteLocation(locationId: number) {
  return sendJson<undefined>(
    "DELETE",
    `/v1/locations/${locationId}`,
    undefined,
    await getAdminToken(),
  );
}
