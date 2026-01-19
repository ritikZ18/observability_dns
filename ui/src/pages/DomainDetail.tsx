import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, BarChart, Bar } from 'recharts';
import { domainsApi, probeRunsApi, incidentsApi } from '../services/api';
import type { DomainDetail, ProbeRun, Incident } from '../types';
import { CheckType } from '../types';
import '../App.css';

export default function DomainDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [domain, setDomain] = useState<DomainDetail | null>(null);
  const [probeRuns, setProbeRuns] = useState<ProbeRun[]>([]);
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'dns' | 'tls' | 'http' | 'incidents'>('overview');

  const loadData = async () => {
    if (!id) return;

    try {
      const [domainData, runsData, incidentsData] = await Promise.all([
        domainsApi.getById(id),
        probeRunsApi.getByDomain(id, undefined, 100),
        incidentsApi.getAll(undefined, id)
      ]);

      setDomain(domainData);
      setProbeRuns(runsData);
      setIncidents(incidentsData);
    } catch (err: any) {
      setError(err.message || 'Failed to load domain details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 30000);
    return () => clearInterval(interval);
  }, [id]);

  const getCheckTypeRuns = (checkType: CheckType): ProbeRun[] => {
    return probeRuns.filter(r => r.checkType === checkType)
      .sort((a, b) => new Date(a.completedAt).getTime() - new Date(b.completedAt).getTime());
  };

  const prepareChartData = (runs: ProbeRun[]) => {
    return runs.map(run => ({
      time: new Date(run.completedAt).toLocaleTimeString(),
      timestamp: new Date(run.completedAt).getTime(),
      latency: run.totalMs || 0,
      success: run.success ? 1 : 0,
      statusCode: run.statusCode || 0
    })).slice(-50); // Last 50 runs
  };

  const getStatusStats = (runs: ProbeRun[]) => {
    const total = runs.length;
    const success = runs.filter(r => r.success).length;
    const failed = total - success;
    const avgLatency = total > 0 
      ? Math.round(runs.reduce((sum, r) => sum + (r.totalMs || 0), 0) / total)
      : 0;
    const successRate = total > 0 ? Math.round((success / total) * 100) : 0;
    
    return { total, success, failed, avgLatency, successRate };
  };

  if (loading) {
    return (
      <div className="app-container">
        <div className="terminal-header">
          <span className="prompt">$</span>
          <span className="command">loading domain details</span>
          <span className="terminal-loading"></span>
        </div>
      </div>
    );
  }

  if (error || !domain) {
    return (
      <div className="app-container">
        <div className="terminal-header">
          <button className="terminal-button" onClick={() => navigate('/')} style={{ marginRight: '1rem' }}>
            ← Back
          </button>
          <span className="prompt">$</span>
          <span className="command">error</span>
        </div>
        <div style={{ padding: '2rem' }}>
          <div className="terminal-error">{error || 'Domain not found'}</div>
        </div>
      </div>
    );
  }

  const dnsRuns = getCheckTypeRuns(CheckType.DNS);
  const tlsRuns = getCheckTypeRuns(CheckType.TLS);
  const httpRuns = getCheckTypeRuns(CheckType.HTTP);

  const dnsStats = getStatusStats(dnsRuns);
  const tlsStats = getStatusStats(tlsRuns);
  const httpStats = getStatusStats(httpRuns);

  const dnsChartData = prepareChartData(dnsRuns);
  const tlsChartData = prepareChartData(tlsRuns);
  const httpChartData = prepareChartData(httpRuns);

  return (
    <div className="app-container">
      <div className="terminal-header">
        <button className="terminal-button" onClick={() => navigate('/')} style={{ marginRight: '1rem' }}>
          ← Back
        </button>
        <span className="prompt">$</span>
        <span className="command">domain detail</span>
        <span className="path">{domain.name}</span>
      </div>

      <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto' }}>
        {/* Domain Header */}
        <div className="terminal-card" style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
            <div>
              <h1 style={{ 
                fontSize: '28px', 
                fontWeight: 600, 
                color: 'var(--cyan)',
                marginBottom: '0.5rem',
                fontFamily: 'JetBrains Mono, monospace'
              }}>
                {domain.name}
              </h1>
              <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                <span style={{ color: 'var(--blue-bright)' }}>interval:</span> {domain.intervalMinutes} minutes
                {' • '}
                <span style={{ color: 'var(--blue-bright)' }}>status:</span>{' '}
                <span style={{ color: domain.enabled ? 'var(--status-success)' : 'var(--status-error)' }}>
                  {domain.enabled ? 'ENABLED' : 'DISABLED'}
                </span>
                {' • '}
                <span style={{ color: 'var(--blue-bright)' }}>created:</span> {new Date(domain.createdAt).toLocaleDateString()}
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div style={{ 
          display: 'flex', 
          gap: '0.5rem', 
          marginBottom: '1.5rem',
          borderBottom: '1px solid var(--border-color)'
        }}>
          {(['overview', 'dns', 'tls', 'http', 'incidents'] as const).map(tab => (
            <button
              key={tab}
              className="terminal-button"
              onClick={() => setActiveTab(tab)}
              style={{
                background: activeTab === tab ? 'var(--bg-tertiary)' : 'transparent',
                border: 'none',
                borderBottom: activeTab === tab ? '2px solid var(--green-bright)' : '2px solid transparent',
                borderRadius: 0,
                textTransform: 'uppercase',
                fontSize: '12px',
                fontWeight: 600,
                padding: '0.75rem 1rem'
              }}
            >
              {tab}
            </button>
          ))}
        </div>

        {/* Overview Tab */}
        {activeTab === 'overview' && (
          <div>
            {/* Summary Stats */}
            <div className="terminal-grid" style={{ marginBottom: '2rem' }}>
              <div className="terminal-card">
                <div className="terminal-card-header">
                  <div className="terminal-card-title">DNS Health</div>
                </div>
                <div style={{ fontSize: '24px', fontWeight: 600, color: 'var(--blue-bright)', marginBottom: '0.5rem' }}>
                  {dnsStats.successRate}%
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                  {dnsStats.success}/{dnsStats.total} successful • Avg: {dnsStats.avgLatency}ms
                </div>
              </div>

              <div className="terminal-card">
                <div className="terminal-card-header">
                  <div className="terminal-card-title">TLS Health</div>
                </div>
                <div style={{ fontSize: '24px', fontWeight: 600, color: 'var(--yellow)', marginBottom: '0.5rem' }}>
                  {tlsStats.successRate}%
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                  {tlsStats.success}/{tlsStats.total} successful • Avg: {tlsStats.avgLatency}ms
                </div>
              </div>

              <div className="terminal-card">
                <div className="terminal-card-header">
                  <div className="terminal-card-title">HTTP Health</div>
                </div>
                <div style={{ fontSize: '24px', fontWeight: 600, color: 'var(--cyan)', marginBottom: '0.5rem' }}>
                  {httpStats.successRate}%
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                  {httpStats.success}/{httpStats.total} successful • Avg: {httpStats.avgLatency}ms
                </div>
              </div>

              <div className="terminal-card">
                <div className="terminal-card-header">
                  <div className="terminal-card-title">Open Incidents</div>
                </div>
                <div style={{ fontSize: '24px', fontWeight: 600, color: 'var(--status-warning)', marginBottom: '0.5rem' }}>
                  {incidents.filter(i => i.status === 0).length}
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                  Active alerts
                </div>
              </div>
            </div>

            {/* Combined Latency Chart */}
            {httpChartData.length > 0 && (
              <div className="terminal-card" style={{ marginBottom: '2rem' }}>
                <div className="terminal-card-header">
                  <div className="terminal-card-title">Latency Trends (Last 50 Runs)</div>
                </div>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={httpChartData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="var(--border-color)" />
                    <XAxis 
                      dataKey="time" 
                      stroke="var(--text-secondary)"
                      style={{ fontSize: '11px' }}
                      tick={{ fill: 'var(--text-secondary)' }}
                    />
                    <YAxis 
                      stroke="var(--text-secondary)"
                      style={{ fontSize: '11px' }}
                      tick={{ fill: 'var(--text-secondary)' }}
                      label={{ value: 'Latency (ms)', angle: -90, position: 'insideLeft', fill: 'var(--text-secondary)' }}
                    />
                    <Tooltip 
                      contentStyle={{ 
                        backgroundColor: 'var(--bg-tertiary)', 
                        border: '1px solid var(--border-color)',
                        color: 'var(--text-primary)',
                        fontSize: '12px'
                      }}
                    />
                    <Legend wrapperStyle={{ fontSize: '12px', color: 'var(--text-secondary)' }} />
                    <Line 
                      type="monotone" 
                      dataKey="latency" 
                      stroke="var(--cyan)" 
                      strokeWidth={2}
                      dot={false}
                      name="HTTP Latency"
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            )}
          </div>
        )}

        {/* DNS Tab */}
        {activeTab === 'dns' && (
          <div>
            {dnsChartData.length > 0 ? (
              <>
                <div className="terminal-card" style={{ marginBottom: '2rem' }}>
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">DNS Resolution Latency</div>
                  </div>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={dnsChartData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="var(--border-color)" />
                      <XAxis 
                        dataKey="time" 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                      />
                      <YAxis 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                        label={{ value: 'Latency (ms)', angle: -90, position: 'insideLeft', fill: 'var(--text-secondary)' }}
                      />
                      <Tooltip 
                        contentStyle={{ 
                          backgroundColor: 'var(--bg-tertiary)', 
                          border: '1px solid var(--border-color)',
                          color: 'var(--text-primary)',
                          fontSize: '12px'
                        }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="latency" 
                        stroke="var(--blue-bright)" 
                        strokeWidth={2}
                        dot={false}
                        name="DNS Latency"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>

                <div className="terminal-card">
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">Recent DNS Probe Runs</div>
                  </div>
                  <div className="terminal-code" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                    {dnsRuns.slice(-20).reverse().map(run => (
                      <div key={run.id} style={{ 
                        padding: '0.5rem 0',
                        borderBottom: '1px solid var(--border-color)',
                        fontSize: '12px',
                        fontFamily: 'JetBrains Mono, monospace'
                      }}>
                        <span style={{ color: 'var(--text-secondary)' }}>
                          [{new Date(run.completedAt).toLocaleString()}]
                        </span>
                        {' '}
                        <span style={{ color: run.success ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {run.success ? '✓' : '✗'}
                        </span>
                        {' '}
                        <span style={{ color: 'var(--blue-bright)' }}>DNS</span>
                        {' '}
                        <span style={{ color: 'var(--text-primary)' }}>
                          {run.totalMs}ms
                        </span>
                        {run.errorMessage && (
                          <div style={{ color: 'var(--status-error)', marginLeft: '2rem', marginTop: '0.25rem' }}>
                            ERROR: {run.errorMessage}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </>
            ) : (
              <div className="terminal-card">
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  No DNS probe runs yet
                </div>
              </div>
            )}
          </div>
        )}

        {/* TLS Tab */}
        {activeTab === 'tls' && (
          <div>
            {tlsChartData.length > 0 ? (
              <>
                <div className="terminal-card" style={{ marginBottom: '2rem' }}>
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">TLS Handshake Latency</div>
                  </div>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={tlsChartData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="var(--border-color)" />
                      <XAxis 
                        dataKey="time" 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                      />
                      <YAxis 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                        label={{ value: 'Latency (ms)', angle: -90, position: 'insideLeft', fill: 'var(--text-secondary)' }}
                      />
                      <Tooltip 
                        contentStyle={{ 
                          backgroundColor: 'var(--bg-tertiary)', 
                          border: '1px solid var(--border-color)',
                          color: 'var(--text-primary)',
                          fontSize: '12px'
                        }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="latency" 
                        stroke="var(--yellow)" 
                        strokeWidth={2}
                        dot={false}
                        name="TLS Latency"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>

                <div className="terminal-card">
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">Recent TLS Probe Runs</div>
                  </div>
                  <div className="terminal-code" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                    {tlsRuns.slice(-20).reverse().map(run => (
                      <div key={run.id} style={{ 
                        padding: '0.5rem 0',
                        borderBottom: '1px solid var(--border-color)',
                        fontSize: '12px',
                        fontFamily: 'JetBrains Mono, monospace'
                      }}>
                        <span style={{ color: 'var(--text-secondary)' }}>
                          [{new Date(run.completedAt).toLocaleString()}]
                        </span>
                        {' '}
                        <span style={{ color: run.success ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {run.success ? '✓' : '✗'}
                        </span>
                        {' '}
                        <span style={{ color: 'var(--yellow)' }}>TLS</span>
                        {' '}
                        <span style={{ color: 'var(--text-primary)' }}>
                          {run.totalMs}ms
                        </span>
                        {run.errorMessage && (
                          <div style={{ color: 'var(--status-error)', marginLeft: '2rem', marginTop: '0.25rem' }}>
                            ERROR: {run.errorMessage}
                          </div>
                        )}
                        {run.certificateInfo && (
                          <div style={{ color: 'var(--text-secondary)', marginLeft: '2rem', marginTop: '0.25rem', fontSize: '11px' }}>
                            {JSON.stringify(run.certificateInfo, null, 2)}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </>
            ) : (
              <div className="terminal-card">
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  No TLS probe runs yet
                </div>
              </div>
            )}
          </div>
        )}

        {/* HTTP Tab */}
        {activeTab === 'http' && (
          <div>
            {httpChartData.length > 0 ? (
              <>
                <div className="terminal-card" style={{ marginBottom: '2rem' }}>
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">HTTP Response Time</div>
                  </div>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={httpChartData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="var(--border-color)" />
                      <XAxis 
                        dataKey="time" 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                      />
                      <YAxis 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                        label={{ value: 'Latency (ms)', angle: -90, position: 'insideLeft', fill: 'var(--text-secondary)' }}
                      />
                      <Tooltip 
                        contentStyle={{ 
                          backgroundColor: 'var(--bg-tertiary)', 
                          border: '1px solid var(--border-color)',
                          color: 'var(--text-primary)',
                          fontSize: '12px'
                        }}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="latency" 
                        stroke="var(--cyan)" 
                        strokeWidth={2}
                        dot={false}
                        name="HTTP Latency"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>

                <div className="terminal-card" style={{ marginBottom: '2rem' }}>
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">HTTP Status Codes</div>
                  </div>
                  <ResponsiveContainer width="100%" height={200}>
                    <BarChart data={httpChartData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="var(--border-color)" />
                      <XAxis 
                        dataKey="time" 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                      />
                      <YAxis 
                        stroke="var(--text-secondary)"
                        style={{ fontSize: '11px' }}
                        tick={{ fill: 'var(--text-secondary)' }}
                      />
                      <Tooltip 
                        contentStyle={{ 
                          backgroundColor: 'var(--bg-tertiary)', 
                          border: '1px solid var(--border-color)',
                          color: 'var(--text-primary)',
                          fontSize: '12px'
                        }}
                      />
                      <Bar dataKey="statusCode" fill="var(--green-bright)" />
                    </BarChart>
                  </ResponsiveContainer>
                </div>

                <div className="terminal-card">
                  <div className="terminal-card-header">
                    <div className="terminal-card-title">Recent HTTP Probe Runs</div>
                  </div>
                  <div className="terminal-code" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                    {httpRuns.slice(-20).reverse().map(run => (
                      <div key={run.id} style={{ 
                        padding: '0.5rem 0',
                        borderBottom: '1px solid var(--border-color)',
                        fontSize: '12px',
                        fontFamily: 'JetBrains Mono, monospace'
                      }}>
                        <span style={{ color: 'var(--text-secondary)' }}>
                          [{new Date(run.completedAt).toLocaleString()}]
                        </span>
                        {' '}
                        <span style={{ color: run.success ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {run.success ? '✓' : '✗'}
                        </span>
                        {' '}
                        <span style={{ color: 'var(--cyan)' }}>HTTP</span>
                        {' '}
                        <span style={{ color: run.statusCode && run.statusCode < 400 ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {run.statusCode || 'N/A'}
                        </span>
                        {' '}
                        <span style={{ color: 'var(--text-primary)' }}>
                          {run.totalMs}ms
                        </span>
                        {run.errorMessage && (
                          <div style={{ color: 'var(--status-error)', marginLeft: '2rem', marginTop: '0.25rem' }}>
                            ERROR: {run.errorMessage}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </>
            ) : (
              <div className="terminal-card">
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  No HTTP probe runs yet
                </div>
              </div>
            )}
          </div>
        )}

        {/* Incidents Tab */}
        {activeTab === 'incidents' && (
          <div>
            {incidents.length > 0 ? (
              <div className="terminal-card">
                <div className="terminal-card-header">
                  <div className="terminal-card-title">Incidents</div>
                </div>
                <table className="terminal-table">
                  <thead>
                    <tr>
                      <th>Started</th>
                      <th>Check Type</th>
                      <th>Severity</th>
                      <th>Status</th>
                      <th>Reason</th>
                      <th>Resolved</th>
                    </tr>
                  </thead>
                  <tbody>
                    {incidents.map(incident => (
                      <tr key={incident.id}>
                        <td>{new Date(incident.startedAt).toLocaleString()}</td>
                        <td>
                          <span className="terminal-badge info">
                            {incident.checkType === CheckType.DNS ? 'DNS' : 
                             incident.checkType === CheckType.TLS ? 'TLS' : 'HTTP'}
                          </span>
                        </td>
                        <td>
                          <span className={`terminal-badge ${
                            incident.severity === 3 ? 'error' :
                            incident.severity === 2 ? 'warning' : 'info'
                          }`}>
                            {incident.severity === 3 ? 'CRITICAL' :
                             incident.severity === 2 ? 'HIGH' :
                             incident.severity === 1 ? 'MEDIUM' : 'LOW'}
                          </span>
                        </td>
                        <td>
                          <span className={`terminal-badge ${incident.status === 0 ? 'error' : 'success'}`}>
                            {incident.status === 0 ? 'OPEN' : 'RESOLVED'}
                          </span>
                        </td>
                        <td style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                          {incident.reason || 'N/A'}
                        </td>
                        <td>
                          {incident.resolvedAt 
                            ? new Date(incident.resolvedAt).toLocaleString()
                            : '-'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="terminal-card">
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  No incidents recorded
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
