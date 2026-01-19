import { useEffect, useState } from 'react';
import { domainsApi, probeRunsApi } from '../services/api';
import type { Domain, ProbeRun } from '../types';
import { CheckType } from '../types';

export default function ObservabilityTable() {
  const [domains, setDomains] = useState<Domain[]>([]);
  const [probeRuns, setProbeRuns] = useState<Map<string, ProbeRun[]>>(new Map());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = async () => {
    try {
      const domainsData = await domainsApi.getAll();
      setDomains(domainsData);

      // Load recent probe runs for each domain
      const runsMap = new Map<string, ProbeRun[]>();
      for (const domain of domainsData) {
        try {
          const runs = await probeRunsApi.getByDomain(domain.id, undefined, 1);
          runsMap.set(domain.id, runs);
        } catch (err) {
          // Ignore errors for individual domain runs
        }
      }
      setProbeRuns(runsMap);
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // Refresh every 30 seconds
    const interval = setInterval(loadData, 30000);
    return () => clearInterval(interval);
  }, []);

  const getLatestRun = (domainId: string, checkType: CheckType): ProbeRun | undefined => {
    const runs = probeRuns.get(domainId) || [];
    return runs.find(r => r.checkType === checkType);
  };

  const getStatusColor = (success: boolean | undefined): string => {
    if (success === undefined) return '#999';
    return success ? '#28a745' : '#dc3545';
  };

  const getStatusText = (success: boolean | undefined): string => {
    if (success === undefined) return 'Unknown';
    return success ? 'Healthy' : 'Unhealthy';
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this domain?')) return;
    try {
      await domainsApi.delete(id);
      await loadData();
    } catch (err: any) {
      alert('Failed to delete domain: ' + (err.message || 'Unknown error'));
    }
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  if (error) {
    return <div style={{ color: 'red' }}>Error: {error}</div>;
  }

  if (domains.length === 0) {
    return <div>No domains configured. Add a domain to start monitoring.</div>;
  }

  return (
    <div>
      <h2>Observability Dashboard</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
        <thead>
          <tr style={{ background: '#f0f0f0' }}>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>Domain</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>Status</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>DNS</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>TLS</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>HTTP</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>Last Check</th>
            <th style={{ padding: '0.75rem', textAlign: 'left', border: '1px solid #ddd' }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {domains.map((domain) => {
            const dnsRun = getLatestRun(domain.id, CheckType.DNS);
            const tlsRun = getLatestRun(domain.id, CheckType.TLS);
            const httpRun = getLatestRun(domain.id, CheckType.HTTP);
            const latestRun = [dnsRun, tlsRun, httpRun]
              .filter(r => r !== undefined)
              .sort((a, b) => new Date(b!.completedAt).getTime() - new Date(a!.completedAt).getTime())[0];

            return (
              <tr key={domain.id}>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  <strong>{domain.name}</strong>
                  <br />
                  <small style={{ color: '#666' }}>
                    Every {domain.intervalMinutes} min
                    {!domain.enabled && ' (Disabled)'}
                  </small>
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  <span style={{ color: getStatusColor(latestRun?.success) }}>
                    {getStatusText(latestRun?.success)}
                  </span>
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  {dnsRun ? (
                    <div>
                      <span style={{ color: getStatusColor(dnsRun.success) }}>
                        {dnsRun.success ? '✓' : '✗'}
                      </span>
                      {dnsRun.totalMs !== undefined && ` ${dnsRun.totalMs}ms`}
                    </div>
                  ) : (
                    <span style={{ color: '#999' }}>-</span>
                  )}
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  {tlsRun ? (
                    <div>
                      <span style={{ color: getStatusColor(tlsRun.success) }}>
                        {tlsRun.success ? '✓' : '✗'}
                      </span>
                      {tlsRun.totalMs !== undefined && ` ${tlsRun.totalMs}ms`}
                    </div>
                  ) : (
                    <span style={{ color: '#999' }}>-</span>
                  )}
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  {httpRun ? (
                    <div>
                      <span style={{ color: getStatusColor(httpRun.success) }}>
                        {httpRun.success ? '✓' : '✗'}
                      </span>
                      {httpRun.statusCode && ` ${httpRun.statusCode}`}
                      {httpRun.totalMs !== undefined && ` (${httpRun.totalMs}ms)`}
                    </div>
                  ) : (
                    <span style={{ color: '#999' }}>-</span>
                  )}
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  {latestRun ? (
                    <div>
                      {new Date(latestRun.completedAt).toLocaleString()}
                    </div>
                  ) : (
                    <span style={{ color: '#999' }}>Never</span>
                  )}
                </td>
                <td style={{ padding: '0.75rem', border: '1px solid #ddd' }}>
                  <button
                    onClick={() => handleDelete(domain.id)}
                    style={{
                      padding: '0.25rem 0.5rem',
                      background: '#dc3545',
                      color: 'white',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      fontSize: '0.875rem'
                    }}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
