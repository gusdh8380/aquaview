import { useEffect, useRef, useState } from "react";

/**
 * Custom hook that polls a fetcher function at a given interval.
 * @param {Function} fetcher - async function returning data
 * @param {number} interval - polling interval in ms (default 3000)
 * @returns {{ data: any, error: Error|null, loading: boolean }}
 */
export default function usePolling(fetcher, interval = 3000) {
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const fetcherRef = useRef(fetcher);

  useEffect(() => {
    fetcherRef.current = fetcher;
  }, [fetcher]);

  useEffect(() => {
    let active = true;

    async function poll() {
      try {
        const result = await fetcherRef.current();
        if (active) {
          setData(result);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err);
      } finally {
        if (active) setLoading(false);
      }
    }

    poll(); // initial fetch
    const id = setInterval(poll, interval);
    return () => {
      active = false;
      clearInterval(id);
    };
  }, [interval]);

  return { data, error, loading };
}
