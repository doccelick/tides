import { useEffect, useLayoutEffect, useRef } from "react";

// Calls onVisible each time the element comes into view, and on mount if it is already in view.
// Clipping by a scrolling parent counts, so a tile scrolled out of a strip is not in view.
export function useOnVisible<T extends Element>(onVisible: () => void) {
  const ref = useRef<T>(null);
  const latestOnVisible = useRef(onVisible);

  useLayoutEffect(() => {
    latestOnVisible.current = onVisible;
  });

  useEffect(() => {
    const element = ref.current;
    if (element === null) return;

    const observer = new IntersectionObserver((entries) => {
      if (entries.at(-1)?.isIntersecting) latestOnVisible.current();
    });
    observer.observe(element);

    return () => observer.disconnect();
  }, []);

  return ref;
}
