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

    const fetchApiHealth = async () => {
      try {
        const response = await fetch("/health", { signal: controller.signal });

        if (!response.ok) {
          throw new Error(`API health check failed with ${response.status}`);
        }

        setStatus("online");
      } catch (error: unknown) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }
        setStatus("offline");
      }
    };

    fetchApiHealth();

    return () => controller.abort();
  }, []);

  const { variant, label } = badges[status];
  return <Badge variant={variant}>{label}</Badge>;
}
