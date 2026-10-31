import { useState, type FormEvent } from "react";
import type { ImportResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { formatImportRange } from "./formatImportRange";
import { importPresets, isImported, resolvePreset } from "./importPresets";
import { createImport } from "./importRequests";
import { RangeSelect } from "./RangeSelect";

interface ImportFormProps {
  locationId: number;
  imports: ImportResponse[];
  onImported: () => void;
}

const defaultPresetLabel = "Next 31 days";

export function ImportForm({ locationId, imports, onImported }: ImportFormProps) {
  const [now] = useState(() => new Date());
  const [presetLabel, setPresetLabel] = useState(defaultPresetLabel);
  const [importing, setImporting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const preset = importPresets.find((p) => p.label === presetLabel);
  const range = preset === undefined ? null : resolvePreset(preset, now);
  const alreadyImported = range !== null && isImported(range, imports);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (range === null) return;

    setImporting(true);
    setMessage(null);
    setError(null);
    const result = await createImport(locationId, range);
    setImporting(false);

    if (!result.ok) {
      setError(result.error.message);
      return;
    }

    const added = result.data.imports.map((created) => formatImportRange(created.from, created.to));
    setMessage(`Imported ${added.join(", ")}`);
    onImported();
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-2">
      <RangeSelect
        now={now}
        value={presetLabel}
        onValueChange={setPresetLabel}
        isImported={(candidate) => isImported(candidate, imports)}
      />
      <Button type="submit" className="h-11" disabled={importing || alreadyImported}>
        {alreadyImported ? "Already imported" : "Import"}
      </Button>
      {message !== null && (
        <p role="status" className="text-sm">
          {message}
        </p>
      )}
      {error !== null && (
        <p role="alert" className="text-sm">
          {error}
        </p>
      )}
    </form>
  );
}
