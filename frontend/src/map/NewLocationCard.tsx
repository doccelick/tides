import { useState, type FormEvent } from "react";
import { XIcon } from "lucide-react";
import type { LocationResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { createImport } from "@/imports/importRequests";
import { createLocation } from "./locationRequests";
import type { PressPosition } from "./useLongPress";

interface NewLocationCardProps {
  position: PressPosition;
  // Called when the location is saved, before its import runs.
  onLocationSaved: (location: LocationResponse) => void;
  // Called when the location is saved and its first import succeeded.
  onCreated: (location: LocationResponse) => void;
  onClose: () => void;
}

// Matches the API's limit on location names.
const nameMaxLength = 100;

export function NewLocationCard({
  position,
  onLocationSaved,
  onCreated,
  onClose,
}: NewLocationCardProps) {
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nameErrors, setNameErrors] = useState<string[]>([]);
  // Set when the location was saved and its import failed, so a retry only imports.
  const [savedLocation, setSavedLocation] = useState<LocationResponse | null>(null);

  async function importMonths(location: LocationResponse) {
    const result = await createImport(location.id);
    if (!result.ok) {
      setSavedLocation(location);
      setError(`Location saved, but the import failed: ${result.error.message}`);
      return;
    }

    onCreated(location);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNameErrors([]);

    try {
      if (savedLocation !== null) {
        await importMonths(savedLocation);
        return;
      }

      const result = await createLocation({
        name: name.trim(),
        latitude: position.latitude,
        longitude: position.longitude,
      });

      if (!result.ok) {
        const errors = result.error.fieldErrors.Name ?? [];
        setNameErrors(errors);
        if (errors.length === 0) setError(result.error.message);
        return;
      }

      onLocationSaved(result.data);
      await importMonths(result.data);
    } finally {
      setSaving(false);
    }
  }

  return (
    <section aria-label="New location" className="border-t bg-background p-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">New location</h2>
        <Button
          variant="ghost"
          size="icon"
          aria-label="Close"
          className="size-11"
          onClick={onClose}
        >
          <XIcon />
        </Button>
      </div>
      <p className="text-sm text-muted-foreground tabular-nums">
        {position.latitude.toFixed(4)}, {position.longitude.toFixed(4)}
      </p>
      <form onSubmit={handleSubmit} className="mt-3 flex flex-col gap-3">
        <label className="flex flex-col gap-1 text-sm">
          Name
          <Input
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
            maxLength={nameMaxLength}
            disabled={savedLocation !== null}
            aria-invalid={nameErrors.length > 0}
            className="h-11"
          />
        </label>
        {nameErrors.map((nameError) => (
          <p key={nameError} className="text-sm text-destructive">
            {nameError}
          </p>
        ))}
        {error !== null && (
          <p role="alert" className="text-sm">
            {error}
          </p>
        )}
        <Button type="submit" className="h-11" disabled={saving}>
          {savedLocation === null ? "Save and import" : "Retry import"}
        </Button>
      </form>
    </section>
  );
}
