import { ChevronDownIcon } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { ImportForm } from "./ImportForm";
import { ImportRow } from "./ImportRow";
import { useImports } from "./useImports";

interface LocationImportsProps {
  locationId: number;
  // Called after the stored tides change, so the card can read them again.
  onChanged: () => void;
}

export function LocationImports({ locationId, onChanged }: LocationImportsProps) {
  const { state, reload } = useImports(locationId);

  function handleChanged() {
    reload();
    onChanged();
  }

  return (
    <section aria-label="Imports" className="mt-3 border-t pt-3">
      <details className="group">
        <summary className="flex h-11 cursor-pointer list-none items-center justify-between text-sm font-semibold [&::-webkit-details-marker]:hidden">
          {state.status === "loaded" ? `Imports (${state.imports.length})` : "Imports"}
          <ChevronDownIcon className="size-4 transition-transform group-open:rotate-180" />
        </summary>
        <div className="flex flex-col gap-2">
          {state.status === "loading" && <Skeleton className="h-10 w-full" />}
          {state.status === "failed" && (
            <p role="alert" className="text-sm">
              Couldn't load imports
            </p>
          )}
          {state.status === "loaded" && state.imports.length === 0 && (
            <p className="text-sm text-muted-foreground">No imports yet.</p>
          )}
          {state.status === "loaded" && state.imports.length > 0 && (
            <ul className="flex flex-col">
              {state.imports.map((stored) => (
                <ImportRow key={stored.id} stored={stored} onDeleted={handleChanged} />
              ))}
            </ul>
          )}
          {state.status === "loaded" && (
            <ImportForm
              locationId={locationId}
              imports={state.imports}
              onImported={handleChanged}
            />
          )}
        </div>
      </details>
    </section>
  );
}
