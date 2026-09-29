import { useEffect, useState } from "react";
import { Badge } from "@/components/ui/badge";

type Status = "checking" | "online" | "offline";

const badges = {
  checking: { variant: "secondary", label: "Checking API…" },
  online: { variant: "default", label: "API online" },
  offline: { variant: "destructive", label: "API offline" },
} as const;

export function ApiStatus() {
  const [status, setStatus] = useState<Status>("checking");

  useEffect(() => {
    // StrictMode mounts twice in development, aborting cancels the first request.
    const controller = new AbortController();

    fetch("/health", { signal: controller.signal })
      .then((response) => setStatus(response.ok ? "online" : "offline"))
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setStatus("offline");
      });

    return () => controller.abort();
  }, []);

  const { variant, label } = badges[status];
  return <Badge variant={variant}>{label}</Badge>;
}
