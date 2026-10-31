import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { EdgeStatus } from "@/tides/useTides";
import { useOnVisible } from "@/tides/useOnVisible";

interface GraphEdgeProps {
  status: EdgeStatus;
  endMessage: string;
  failedMessage: string;
  onLoad: () => void;
}

const edgeClassName = "w-24 shrink-0 self-center px-2 text-xs";

// One end of the graph strip, which loads more days when it scrolls into view.
export function GraphEdge({ status, endMessage, failedMessage, onLoad }: GraphEdgeProps) {
  if (status === "end") {
    return <p className={`${edgeClassName} text-muted-foreground`}>{endMessage}</p>;
  }
  if (status === "failed") {
    return (
      <div className={edgeClassName}>
        <p role="alert">{failedMessage}</p>
        <Button variant="outline" size="sm" className="mt-2" onClick={onLoad}>
          Retry
        </Button>
      </div>
    );
  }
  return <LoadingEdge onVisible={onLoad} />;
}

function LoadingEdge({ onVisible }: { onVisible: () => void }) {
  const ref = useOnVisible<HTMLDivElement>(onVisible);
  return <Skeleton ref={ref} className="w-8 shrink-0" />;
}
