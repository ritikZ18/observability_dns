import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import DomainForm from '../components/DomainForm';
import ObservabilityTable from '../components/ObservabilityTable';
import { domainsApi, probeRunsApi, incidentsApi } from '../services/api';
import { IncidentStatus } from '../types';
import '../App.css';

export default function Dashboard() {
  const navigate = useNavigate();
  const [summary, setSummary] = useState({
    totalDomains: 0,
    healthyDomains: 0,
    unhealthyDomains: 0,
    openIncidents: 0,
    totalProbeRuns: 0
  });
  const [loading, setLoading] = useState(true);

  const loadSummary = async () => {
    try {
      const domainsData = await domainsApi.getAll();

      // Load recent probe runs for summary
      let healthyCount = 0;
      let unhealthyCount = 0;
      let totalRuns = 0;

      for (const domain of domainsData) {
        try {
          const runs = await probeRunsApi.getByDomain(domain.id, undefined, 1);
          if (runs.length > 0) {
            totalRuns += runs.length;
            const latestRun = runs[0];
            if (latestRun.success) {
              healthyCount++;
            } else {
              unhealthyCount++;
            }
          } else {
            unhealthyCount++; // No runs yet = unknown
          }
        } catch (err) {
          unhealthyCount++;
        }
      }

      // Load open incidents
      const incidents = await incidentsApi.getAll(IncidentStatus.OPEN);
      const openIncidents = incidents.length;

      setSummary({
        totalDomains: domainsData.length,
        healthyDomains: healthyCount,
        unhealthyDomains: unhealthyCount,
        openIncidents,
        totalProbeRuns: totalRuns
      });
    } catch (err: any) {
      console.error('Failed to load summary:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSummary();
    const interval = setInterval(loadSummary, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const handleDomainAdded = () => {
    loadSummary();
  };

  if (loading) {
    return (
      <div className="app-container">
        <div className="terminal-header">
          <span className="prompt">$</span>
          <span className="command">loading observability dashboard</span>
          <span className="terminal-loading"></span>
        </div>
      </div>
    );
  }

  return (
    <div className="app-container">
      <div className="terminal-header">
        <span className="prompt">$</span>
        <span className="command">dns-tls-observatory</span>
        <span className="path">~/dashboard</span>
        <span style={{ marginLeft: 'auto', color: 'var(--text-secondary)', fontSize: '12px' }}>
          {new Date().toLocaleTimeString()}
        </span>
      </div>

      <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto' }}>
        {/* Terminal prompt header */}
        <div style={{ marginBottom: '2rem' }}>
          <h1 style={{ 
            fontSize: '24px', 
            fontWeight: 600, 
            color: 'var(--green-bright)',
            marginBottom: '0.5rem',
            fontFamily: 'JetBrains Mono, monospace'
          }}>
            <span style={{ color: 'var(--blue-bright)' }}>DNS</span>
            <span style={{ color: 'var(--text-secondary)' }}> & </span>
            <span style={{ color: 'var(--yellow)' }}>TLS</span>
            <span style={{ color: 'var(--text-secondary)' }}> </span>
            <span style={{ color: 'var(--cyan)' }}>Observatory</span>
          </h1>
          <div style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>
            Real-time network observability and monitoring dashboard
          </div>
        </div>

        {/* Summary Cards */}
        <div className="terminal-grid">
          <div className="terminal-card">
            <div className="terminal-card-header">
              <div className="terminal-card-title">Total Domains</div>
            </div>
            <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--blue-bright)' }}>
              {summary.totalDomains}
            </div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Monitored domains
            </div>
          </div>

          <div className="terminal-card">
            <div className="terminal-card-header">
              <div className="terminal-card-title">Healthy</div>
            </div>
            <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--status-success)' }}>
              {summary.healthyDomains}
            </div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Passing all checks
            </div>
          </div>

          <div className="terminal-card">
            <div className="terminal-card-header">
              <div className="terminal-card-title">Unhealthy</div>
            </div>
            <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--status-error)' }}>
              {summary.unhealthyDomains}
            </div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Issues detected
            </div>
          </div>

          <div className="terminal-card">
            <div className="terminal-card-header">
              <div className="terminal-card-title">Open Incidents</div>
            </div>
            <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--status-warning)' }}>
              {summary.openIncidents}
            </div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
              Active alerts
            </div>
          </div>
        </div>

        {/* Add Domain Form */}
        <div className="terminal-card" style={{ marginBottom: '2rem' }}>
          <div className="terminal-card-header">
            <div className="terminal-card-title">Add Domain to Monitor</div>
          </div>
          <DomainForm onDomainAdded={handleDomainAdded} />
        </div>

        {/* Observability Table */}
        <div className="terminal-card">
          <div className="terminal-card-header">
            <div className="terminal-card-title">Observability Dashboard</div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              Click any domain for detailed view
            </div>
          </div>
          <ObservabilityTable onDomainClick={(domainId) => navigate(`/domain/${domainId}`)} />
        </div>
      </div>
    </div>
  );
}
