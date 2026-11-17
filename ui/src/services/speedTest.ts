/**
 * Browser-side measurement runner.
 * Requests are relative to the UI origin so the existing Vite/nginx /api proxy
 * sends them to the API without CORS changes.
 *
 * These are HTTP application-layer measurements, not ICMP/UDP diagnostics.
 */
export type SpeedSample = {
  phase: 'latency' | 'download' | 'upload';
  bytes: number;
  elapsedMs: number;
  mbps?: number;
  latencyMs?: number;
};

export type SpeedResult = {
  testedAt: string;
  downloadMbps: number;
  uploadMbps: number;
  idleLatencyMs: number;
  jitterMs: number;
  downloadLoadedLatencyMs: number | null;
  uploadLoadedLatencyMs: number | null;
  packetLossPercent: null;
  samples: SpeedSample[];
};

export type TestProgress = {
  phase: 'latency' | 'download' | 'upload' | 'done';
  completed: number;
  total: number;
  samples: SpeedSample[];
};

const BASE = '/api/speed-test';
const DOWNLOAD_SIZES = [512 * 1024, 2 * 1024 * 1024, 8 * 1024 * 1024];
const UPLOAD_SIZES = [256 * 1024, 1024 * 1024, 4 * 1024 * 1024];

function checkAbort(signal: AbortSignal) {
  if (signal.aborted) throw new DOMException('Speed test cancelled', 'AbortError');
}

function median(values: number[]) {
  if (!values.length) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 === 0
    ? (sorted[middle - 1] + sorted[middle]) / 2
    : sorted[middle];
}

function percentile(values: number[], fraction: number) {
  if (!values.length) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const position = Math.min(sorted.length - 1, Math.max(0, (sorted.length - 1) * fraction));
  const lower = Math.floor(position);
  const upper = Math.ceil(position);
  return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
}

function jitterFromLatencies(values: number[]) {
  if (values.length < 2) return 0;
  let sum = 0;
  for (let i = 1; i < values.length; i++) {
    sum += Math.abs(values[i] - values[i - 1]);
  }
  return sum / (values.length - 1);
}

function speedMbps(bytes: number, durationMs: number) {
  return (bytes * 8) / (Math.max(durationMs, 1) * 1000);
}

async function requestWithTimeout(
  url: string,
  options: RequestInit,
  signal: AbortSignal,
  timeoutMs = 20000
) {
  checkAbort(signal);
  const controller = new AbortController();
  const onAbort = () => controller.abort();
  signal.addEventListener('abort', onAbort, { once: true });
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, { ...options, signal: controller.signal, cache: 'no-store' });
  } catch (error) {
    if (signal.aborted) throw new DOMException('Speed test cancelled', 'AbortError');
    if (controller.signal.aborted) throw new Error('Measurement timed out. Try again.');
    throw error;
  } finally {
    clearTimeout(timeout);
    signal.removeEventListener('abort', onAbort);
  }
}

async function ping(signal: AbortSignal) {
  const started = performance.now();
  const response = await requestWithTimeout(
    BASE + '/ping?nonce=' + Math.random().toString(36).slice(2),
    { method: 'GET' },
    signal,
    8000
  );
  if (!response.ok) throw new Error('Ping request failed (' + response.status + ')');
  return performance.now() - started;
}

async function download(bytes: number, signal: AbortSignal): Promise<SpeedSample> {
  const started = performance.now();
  const response = await requestWithTimeout(
    BASE + '/download?bytes=' + bytes + '&nonce=' + Math.random().toString(36).slice(2),
    { method: 'GET' },
    signal
  );
  if (!response.ok) throw new Error('Download request failed (' + response.status + ')');
  const body = await response.arrayBuffer();
  const elapsedMs = performance.now() - started;
  if (body.byteLength !== bytes) throw new Error('Incomplete download. Received ' + body.byteLength + ' bytes.');
  return { phase: 'download', bytes, elapsedMs, mbps: speedMbps(bytes, elapsedMs) };
}

function randomBytes(bytes: number) {
  const body = new Uint8Array(bytes);
  // The Web Crypto API limits getRandomValues to 65,536 bytes per invocation.
  for (let offset = 0; offset < bytes; offset += 65536) {
    crypto.getRandomValues(body.subarray(offset, Math.min(bytes, offset + 65536)));
  }
  return body;
}

async function upload(bytes: number, signal: AbortSignal): Promise<SpeedSample> {
  checkAbort(signal);
  const body = randomBytes(bytes);
  const started = performance.now();
  const response = await requestWithTimeout(
    BASE + '/upload',
    { method: 'POST', headers: { 'Content-Type': 'application/octet-stream' }, body },
    signal
  );
  const elapsedMs = performance.now() - started;
  if (!response.ok) throw new Error('Upload request failed (' + response.status + ')');
  const summary: { bytesReceived?: number } = await response.json();
  if (summary.bytesReceived !== bytes) throw new Error('Incomplete upload acknowledgement.');
  return { phase: 'upload', bytes, elapsedMs, mbps: speedMbps(bytes, elapsedMs) };
}

/**
 * Executes tests from the visitor's browser, not from DNS/TLS worker nodes.
 * Results are in memory only; no IP addresses or throughput data are persisted.
 */
export async function runSpeedTest(
  signal: AbortSignal,
  onProgress: (progress: TestProgress) => void
): Promise<SpeedResult> {
  const samples: SpeedSample[] = [];
  const total = 8 + DOWNLOAD_SIZES.length + UPLOAD_SIZES.length;
  let completed = 0;
  const notify = (phase: TestProgress['phase']) =>
    onProgress({ phase, completed, total, samples: [...samples] });

  notify('latency');
  const idle: number[] = [];
  for (let i = 0; i < 8; i++) {
    const latencyMs = await ping(signal);
    idle.push(latencyMs);
    samples.push({ phase: 'latency', bytes: 0, elapsedMs: latencyMs, latencyMs });
    completed++;
    notify('latency');
  }

  // Probe once while a download is active, rather than calling idle latency loaded.
  let downloadLoadedLatencyMs: number | null = null;
  for (let i = 0; i < DOWNLOAD_SIZES.length; i++) {
    const load = download(DOWNLOAD_SIZES[i], signal);
    const loadedPing = ping(signal).catch(() => null);
    const sample = await load;
    if (i === DOWNLOAD_SIZES.length - 1) downloadLoadedLatencyMs = await loadedPing;
    else await loadedPing;
    samples.push(sample);
    completed++;
    notify('download');
  }

  let uploadLoadedLatencyMs: number | null = null;
  for (let i = 0; i < UPLOAD_SIZES.length; i++) {
    const load = upload(UPLOAD_SIZES[i], signal);
    const loadedPing = ping(signal).catch(() => null);
    const sample = await load;
    if (i === UPLOAD_SIZES.length - 1) uploadLoadedLatencyMs = await loadedPing;
    else await loadedPing;
    samples.push(sample);
    completed++;
    notify('upload');
  }

  notify('done');
  return {
    testedAt: new Date().toISOString(),
    downloadMbps: percentile(samples.filter(s => s.phase === 'download').map(s => s.mbps ?? 0), 0.9),
    uploadMbps: percentile(samples.filter(s => s.phase === 'upload').map(s => s.mbps ?? 0), 0.9),
    idleLatencyMs: median(idle),
    jitterMs: jitterFromLatencies(idle),
    downloadLoadedLatencyMs,
    uploadLoadedLatencyMs,
    packetLossPercent: null,
    samples
  };
}
