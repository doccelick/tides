import type { CreateLocationRequest, LocationResponse } from "@/api/generated";
import { sendJson } from "@/api/sendJson";

export function createLocation(request: CreateLocationRequest) {
  return sendJson<LocationResponse>("POST", "/v1/locations", request);
}

export function deleteLocation(locationId: number) {
  return sendJson<undefined>("DELETE", `/v1/locations/${locationId}`);
}
