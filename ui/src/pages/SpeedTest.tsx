import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { isLocalSpeedTest, runSpeedTest, type SpeedResult, type TestProgress } from '../services/speedTest';
import '../App.css';
import './SpeedTest.css';

const initialProgress: TestProgress = {
  phase: 'latency',
  completed: 0,
  total: 14,
  samples: []
};

function metric(value: number | null, unit: string, precision = 1) {
  return value === null ? '—' : value.toFixed(precision) + ' ' + unit;
}

export default function SpeedTest() {
  const localMeasurement = isLocalSpeedTest();
  const [progress, setProgress] = useState<TestProgress>(initialProgress);
  const [result, setResult] = useState<SpeedResult | null>(null);
  const [status, setStatus] = useState<'idle' | 'running' | 'success' | 'error' | 'cancelled'>('idle');
  const [error, setError] = useState<string | null>(null);
  const controllerRef = useRef<AbortController | null>(null);
  const activeRef = useRef(false);

  useEffect(() => {
    return () => {
      activeRef.current = false;
      controllerRef.current?.abort();
    };
  }, []);

  async function start() {
    if (activeRef.current) return;
    const controller = new AbortController();
    controllerRef.current = controller;
    activeRef.current = true;
    setProgress(initialProgress);
    setError(null);
    setResult(null);
    setStatus('running');

    try {
      const summary = await runSpeedTest(controller.signal, setProgress);
      if (!controller.signal.aborted) {
        setResult(summary);
        setStatus('success');
      }
    } catch (err) {
      if (controller.signal.aborted) {
        setStatus('cancelled');
      } else {
        setStatus('error');
        setError(err instanceof Error ? err.message : 'An unexpected measurement error occurred.');
      }
    } finally {
      activeRef.current = false;
      if (controllerRef.current === controller) controllerRef.current = null;
    }
  }

  function cancel() {
    controllerRef.current?.abort();
  }

  const downloadSamples = progress.samples.filter(sample => sample.phase === 'download');
  const uploadSamples = progress.samples.filter(sample => sample.phase === 'upload');
  const latencySamples = progress.samples.filter(sample => sample.phase === 'latency');
  const latestDownload = downloadSamples.length ? downloadSamples[downloadSamples.length - 1].mbps ?? null : null;
  const latestUpload = uploadSamples.length ? uploadSamples[uploadSamples.length - 1].mbps ?? null : null;
  const latency = result?.idleLatencyMs ?? (latencySamples.length
    ? latencySamples.reduce((sum, item) => sum + (item.latencyMs ?? 0), 0) / latencySamples.length : null);

  const throughput = progress.samples
    .filter(sample => sample.mbps !== undefined)
    .map((sample, index) => ({
      sample: index + 1,
      download: sample.phase === 'download' ? Number(sample.mbps?.toFixed(2)) : null,
      upload: sample.phase === 'upload' ? Number(sample.mbps?.toFixed(2)) : null
    }));

  return (
    <div className="app-container">
      <div className="terminal-header">
        <span className="prompt">$</span>
        <span className="command">dns-tls-observatory</span>
        <span className="path">~/speed-test</span>
        <Link to="/" className="speed-back">← Dashboard</Link>
      </div>

      <main className="speed-layout">
        <div className="speed-heading">
          <div>
            <p className="speed-kicker">NETWORK DIAGNOSTICS / CLIENT SIDE</p>
            <h1>Current Device Speed Test</h1>
            <p>Measures the connection from this browser to the Observatory server—not from your website-monitoring worker.</p>
          </div>
          <span className="speed-mode">{localMeasurement ? 'BROWSER → LOCAL API' : 'BROWSER → INTERNET'}</span>
        </div>

        <section className="terminal-card speed-controls">
          <div>
            <strong>{status === 'running' ? 'Testing ' + progress.phase + '…' : status === 'success' ? 'Measurement complete' : 'Ready to test your connection'}</strong>
            <p className="speed-muted">
              {status === 'running'
                ? 'Request ' + progress.completed + ' / ' + progress.total + ' • keep this tab active'
                : localMeasurement
                  ? 'Local mode tests browser-to-API throughput on this host; it is not an Internet bandwidth result.'
                  : 'This uses real download/upload traffic. Your network plan may count the test data.'}
            </p>
          </div>
          <div className="speed-control-buttons">
            {status === 'running' ? (
              <button type="button" className="terminal-button danger" onClick={cancel}>STOP TEST</button>
            ) : (
              <button type="button" className="terminal-button primary" onClick={start}>
                {status === 'success' ? 'RETEST CONNECTION' : 'START SPEED TEST'}
              </button>
            )}
          </div>
          <div className="speed-progress-track" role="progressbar" aria-valuenow={progress.completed} aria-valuemin={0} aria-valuemax={progress.total}>
            <div style={{ width: status === 'idle' ? '0%' : (progress.completed / progress.total * 100) + '%' }} />
          </div>
        </section>

        {error && <div role="alert" className="terminal-error">{error} Check that the API is reachable via /api and try again.</div>}
        {status === 'cancelled' && <div role="status" className="speed-message">Test cancelled. No partial speed figures are shown as final results.</div>}

        <section className="speed-metrics" aria-label="Network measurement results">
          <div className="terminal-card speed-metric">
            <span>DOWNLOAD SPEED</span>
            <strong>{metric(result?.downloadMbps ?? latestDownload, 'Mbps')}</strong>
            <small>Effective HTTP download throughput</small>
          </div>
          <div className="terminal-card speed-metric">
            <span>UPLOAD SPEED</span>
            <strong>{metric(result?.uploadMbps ?? latestUpload, 'Mbps')}</strong>
            <small>Effective HTTP upload throughput</small>
          </div>
          <div className="terminal-card speed-metric">
            <span>IDLE LATENCY</span>
            <strong>{metric(latency, 'ms')}</strong>
            <small>Median round-trip HTTP timing</small>
          </div>
          <div className="terminal-card speed-metric">
            <span>JITTER</span>
            <strong>{metric(result?.jitterMs ?? null, 'ms')}</strong>
            <small>Mean consecutive latency difference</small>
          </div>
        </section>

        <section className="speed-details">
          <div className="terminal-card">
            <div className="terminal-card-header"><div className="terminal-card-title">Throughput measurements</div></div>
            {throughput.length ? (
              <div className="speed-chart">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={throughput}>
                    <CartesianGrid stroke="var(--border-color)" strokeDasharray="3 3" />
                    <XAxis dataKey="sample" stroke="var(--text-secondary)" />
                    <YAxis stroke="var(--text-secondary)" unit=" Mbps" width={76} />
                    <Tooltip contentStyle={{ background: '#252526', border: '1px solid #3e3e42', color: '#d4d4d4' }} />
                    <Line type="linear" dataKey="download" name="Download" stroke="#4fc1ff" strokeWidth={2} connectNulls={false} />
                    <Line type="linear" dataKey="upload" name="Upload" stroke="#89d185" strokeWidth={2} connectNulls={false} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            ) : <p className="speed-empty">Start a test to see the throughput graph.</p>}
          </div>

          <div className="terminal-card">
            <div className="terminal-card-header"><div className="terminal-card-title">Connection diagnostics</div></div>
            <dl className="speed-diagnostics">
              <div><dt>Measurement point</dt><dd>This browser</dd></div>
              <div><dt>Server</dt><dd>{localMeasurement ? 'Local Observatory API (/api proxy)' : 'Observatory measurement API'}</dd></div>
              <div><dt>Loaded latency / download</dt><dd>{metric(result?.downloadLoadedLatencyMs ?? null, 'ms')}</dd></div>
              <div><dt>Loaded latency / upload</dt><dd>{metric(result?.uploadLoadedLatencyMs ?? null, 'ms')}</dd></div>
              <div><dt>Packet loss</dt><dd>Not available (requires WebRTC/UDP)</dd></div>
              <div><dt>Test time</dt><dd>{result ? new Date(result.testedAt).toLocaleString() : '—'}</dd></div>
              <div><dt>Browser online flag</dt><dd>{navigator.onLine ? 'Online (reported)' : 'Offline (reported)'}</dd></div>
            </dl>
          </div>
        </section>

        <div className="terminal-card speed-note">
          <div className="terminal-card-title">Measurement limitations</div>
          <p>This test measures HTTP application-layer transfer speed, not your router's independent WAN speed or local LAN bandwidth. Results may be constrained by the API server, proxy, hosting egress limits, and current traffic. The unloaded/loaded timings are HTTP round trips, not ICMP pings. These short samples are an approximation; repeat tests for better comparisons.</p>
          <p>Results remain in browser memory only. No speed-test history is saved to PostgreSQL in this first version.</p>
        </div>
      </main>
    </div>
  );
}
