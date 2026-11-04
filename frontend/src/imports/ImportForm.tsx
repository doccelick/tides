import { useState, type FormEvent } from "react";
import type { ImportResponse } from "@/api/generated";
import { Button } from "@/components/ui/button";
import { formatImportRange } from "./formatImportRange";
import { createImport } from "./importRequests";
import { importWindow, isImported } from "./importWindow";

interface ImportFormProps {
  locationId: number;
  imports: ImportResponse[];
  onImported: () => void;
}

export function ImportForm({ locationId, imports, onImported }: ImportFormProps) {
  const [now] = useState(() => new Date());
  const [importing, setImporting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const allImported = isImported(importWindow(now), imports);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    setImporting(true);
    setMessage(null);
    setError(null);
    const result = await createImport(locationId);
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
      <Button type="submit" className="h-11" disabled={importing || allImported}>
        {allImported ? "All months imported" : "Import missing months"}
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
