import type { EdgeStatus } from "./useTides";
import { TideTileRetry, TideTileSkeleton } from "./TideTile";
import { useOnVisible } from "./useOnVisible";

interface EdgeTileProps {
  status: EdgeStatus;
  failedMessage: string;
  onLoad: () => void;
}

// The tile at one end of the strip, which loads more days when it scrolls into view.
export function EdgeTile({ status, failedMessage, onLoad }: EdgeTileProps) {
  if (status === "end") return null;
  if (status === "failed") return <TideTileRetry message={failedMessage} onRetry={onLoad} />;
  return <LoadingTile onVisible={onLoad} />;
}

function LoadingTile({ onVisible }: { onVisible: () => void }) {
  const ref = useOnVisible<HTMLLIElement>(onVisible);
  return <TideTileSkeleton ref={ref} />;
}
