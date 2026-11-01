import { useEffect, useLayoutEffect, useRef } from "react";

// Calls onVisible each time the element comes within one strip width of the visible part of its
// parent strip, and on mount if it already is. The margin starts loading before the edge scrolls into view.
export function useOnVisible<T extends Element>(onVisible: () => void) {
  const ref = useRef<T>(null);
  const latestOnVisible = useRef(onVisible);

  useLayoutEffect(() => {
    latestOnVisible.current = onVisible;
  });

  useEffect(() => {
    const element = ref.current;
    if (element === null) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.at(-1)?.isIntersecting) latestOnVisible.current();
      },
      { root: element.parentElement, rootMargin: "0px 100%" },
    );
    observer.observe(element);

    return () => observer.disconnect();
  }, []);

  return ref;
}
