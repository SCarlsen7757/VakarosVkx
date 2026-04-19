// Time-based decimation: keep at most `maxPoints` evenly spaced samples.
export function downsample<T extends { t: number }>(data: T[], maxPoints = 2000): T[] {
  if (data.length <= maxPoints) return data;
  const step = data.length / maxPoints;
  const out: T[] = [];
  for (let i = 0; i < maxPoints; i++) {
    out.push(data[Math.floor(i * step)]);
  }
  if (out[out.length - 1] !== data[data.length - 1]) out.push(data[data.length - 1]);
  return out;
}

export function forwardFill<T>(values: (T | null | undefined)[]): (T | null)[] {
  let last: T | null = null;
  return values.map((v) => {
    if (v != null) { last = v; return v; }
    return last;
  });
}
