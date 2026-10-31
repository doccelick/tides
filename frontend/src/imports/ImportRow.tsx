import { useState } from "react";
import { Trash2Icon } from "lucide-react";
import type { ImportResponse } from "@/api/generated";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { formatImportRange } from "./formatImportRange";
import { deleteImport } from "./importRequests";

interface ImportRowProps {
  stored: ImportResponse;
  onDeleted: () => void;
}

const countFormat = new Intl.NumberFormat("en-GB");

export function ImportRow({ stored, onDeleted }: ImportRowProps) {
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const range = formatImportRange(stored.from, stored.to);

  async function handleDelete() {
    setDeleting(true);
    setError(null);
    const result = await deleteImport(stored.id);
    setDeleting(false);

    if (!result.ok) {
      setError(result.error.message);
      return;
    }

    setOpen(false);
    onDeleted();
  }

  return (
    <li className="flex items-center justify-between gap-2">
      <div>
        <p className="text-sm">{range}</p>
        <p className="text-xs text-muted-foreground">
          {countFormat.format(stored.entryCount)} entries
        </p>
      </div>
      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogTrigger
          render={
            <Button
              variant="ghost"
              size="icon"
              className="size-11"
              aria-label={`Delete ${range}`}
            />
          }
        >
          <Trash2Icon />
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this import?</AlertDialogTitle>
            <AlertDialogDescription>
              The stored tides for {range} are deleted. The card then fetches those days from
              Kartverket.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {error !== null && (
            <p role="alert" className="text-sm">
              {error}
            </p>
          )}
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction variant="destructive" disabled={deleting} onClick={handleDelete}>
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </li>
  );
}
