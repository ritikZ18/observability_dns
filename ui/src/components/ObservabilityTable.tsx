import { useEffect, useState } from 'react';
import { domainsApi, probeRunsApi } from '../services/api';
import type { Domain, ProbeRun } from '../types';
import { CheckType } from '../types';
import '../App.css';

interface ObservabilityTableProps {
  onDomainClick?: (domainId: string) => void;
}

export default function ObservabilityTable({ onDomainClick }: ObservabilityTableProps) {
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
          const runs = await probeRunsApi.getByDomain(domain.id, undefined, 3);
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

  const getStatusIndicator = (success: boolean | undefined): string => {
    if (success === undefined) return 'unknown';
    return success ? 'success' : 'error';
  };

  const getStatusText = (success: boolean | undefined): string => {
    if (success === undefined) return 'Unknown';
    return success ? 'OK' : 'FAIL';
  };

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation(); // Prevent row click
    if (!confirm('Are you sure you want to delete this domain?')) return;
    try {
      await domainsApi.delete(id);
      await loadData();
    } catch (err: any) {
      alert('Failed to delete domain: ' + (err.message || 'Unknown error'));
    }
  };

  if (loading) {
    return <div className="terminal-loading">Loading observability data</div>;
  }

  if (error) {
    return <div className="terminal-error">Error: {error}</div>;
  }

  if (domains.length === 0) {
    return (
      <div style={{ 
        padding: '2rem', 
        textAlign: 'center', 
        color: 'var(--text-secondary)',
        fontSize: '13px'
      }}>
        <div style={{ marginBottom: '0.5rem' }}>No domains configured.</div>
        <div>Add a domain above to start monitoring.</div>
      </div>
    );
  }

  return (
    <div>
      <table className="terminal-table">
        <thead>
          <tr>
            <th>Domain</th>
            <th>DNS</th>
            <th>TLS</th>
            <th>HTTP</th>
            <th>Status</th>
            <th>Last Check</th>
            <th style={{ textAlign: 'right' }}>Actions</th>
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

            const overallSuccess = latestRun?.success ?? undefined;
            const statusIndicator = getStatusIndicator(overallSuccess);

            return (
              <tr 
                key={domain.id}
                onClick={() => onDomainClick?.(domain.id)}
                style={{ cursor: onDomainClick ? 'pointer' : 'default' }}
              >
                <td>
                  <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                    {domain.name}
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
                    <span style={{ color: 'var(--blue-bright)' }}>interval:</span> {domain.intervalMinutes}m
                    {!domain.enabled && (
                      <span style={{ color: 'var(--status-warning)', marginLeft: '0.5rem' }}>
                        [DISABLED]
                      </span>
                    )}
                  </div>
                </td>
                <td>
                  {dnsRun ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span className={`status-indicator ${getStatusIndicator(dnsRun.success)}`}></span>
                      <span style={{ 
                        color: dnsRun.success ? 'var(--status-success)' : 'var(--status-error)',
                        fontSize: '12px',
                        fontWeight: 500
                      }}>
                        {dnsRun.success ? 'OK' : 'FAIL'}
                      </span>
                      {dnsRun.totalMs !== undefined && (
                        <span style={{ color: 'var(--text-secondary)', fontSize: '11px' }}>
                          {dnsRun.totalMs}ms
                        </span>
                      )}
                    </div>
                  ) : (
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>-</span>
                  )}
                </td>
                <td>
                  {tlsRun ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span className={`status-indicator ${getStatusIndicator(tlsRun.success)}`}></span>
                      <span style={{ 
                        color: tlsRun.success ? 'var(--status-success)' : 'var(--status-error)',
                        fontSize: '12px',
                        fontWeight: 500
                      }}>
                        {tlsRun.success ? 'OK' : 'FAIL'}
                      </span>
                      {tlsRun.totalMs !== undefined && (
                        <span style={{ color: 'var(--text-secondary)', fontSize: '11px' }}>
                          {tlsRun.totalMs}ms
                        </span>
                      )}
                    </div>
                  ) : (
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>-</span>
                  )}
                </td>
                <td>
                  {httpRun ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span className={`status-indicator ${getStatusIndicator(httpRun.success)}`}></span>
                      <span style={{ 
                        color: httpRun.success ? 'var(--status-success)' : 'var(--status-error)',
                        fontSize: '12px',
                        fontWeight: 500
                      }}>
                        {httpRun.statusCode || (httpRun.success ? 'OK' : 'FAIL')}
                      </span>
                      {httpRun.totalMs !== undefined && (
                        <span style={{ color: 'var(--text-secondary)', fontSize: '11px' }}>
                          {httpRun.totalMs}ms
                        </span>
                      )}
                    </div>
                  ) : (
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>-</span>
                  )}
                </td>
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <span className={`status-indicator ${statusIndicator}`}></span>
                    <span className={`terminal-badge ${statusIndicator === 'success' ? 'success' : 'error'}`}>
                      {getStatusText(overallSuccess)}
                    </span>
                  </div>
                </td>
                <td>
                  {latestRun ? (
                    <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                      {new Date(latestRun.completedAt).toLocaleString()}
                    </div>
                  ) : (
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Never</span>
                  )}
                </td>
                <td style={{ textAlign: 'right' }}>
                  <button
                    className="terminal-button danger"
                    style={{ fontSize: '11px', padding: '0.25rem 0.5rem' }}
                    onClick={(e) => handleDelete(e, domain.id)}
                  >
                    DELETE
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
