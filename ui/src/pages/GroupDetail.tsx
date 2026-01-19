import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { groupsApi, domainsApi, probeRunsApi, incidentsApi } from '../services/api';
import type { GroupDetail as GroupDetailType, Domain, ProbeRun } from '../types';
import { IncidentStatus, CheckType } from '../types';
import '../App.css';

export default function GroupDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [group, setGroup] = useState<GroupDetailType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [domainsStats, setDomainsStats] = useState<Map<string, any>>(new Map());

  useEffect(() => {
    if (!id) return;

    const loadGroup = async () => {
      try {
        setLoading(true);
        const groupData = await groupsApi.getById(id);
        setGroup(groupData);

        // Load stats for each domain
        const statsMap = new Map();
        for (const domain of groupData.domains) {
          try {
            const runs = await probeRunsApi.getByDomain(domain.id, undefined, 10);
            const recentRuns = runs.slice(0, 10);
            const successfulRuns = recentRuns.filter(r => r.success).length;
            const failedRuns = recentRuns.length - successfulRuns;
            const avgLatency = recentRuns.length > 0
              ? Math.round(recentRuns.reduce((sum, r) => sum + (r.totalMs || 0), 0) / recentRuns.length)
              : 0;

            statsMap.set(domain.id, {
              totalRuns: recentRuns.length,
              successfulRuns,
              failedRuns,
              avgLatency,
              lastRun: recentRuns[0]?.completedAt
            });
          } catch (err) {
            // Ignore errors for individual domains
          }
        }
        setDomainsStats(statsMap);
      } catch (err: any) {
        setError(err.message || 'Failed to load group');
      } finally {
        setLoading(false);
      }
    };

    loadGroup();
  }, [id]);

  if (loading) {
    return (
      <div className="app-container">
        <div className="terminal-loading-full">Loading group details</div>
      </div>
    );
  }

  if (error || !group) {
    return (
      <div className="app-container">
        <div className="terminal-error">{error || 'Group not found'}</div>
      </div>
    );
  }

  const stats = group.statistics;

  return (
    <div className="app-container">
      <div className="terminal-header">
        <span className="prompt">$</span>
        <span className="command">group-detail</span>
        <span className="path">~/groups/{group.name}</span>
        <button
          className="terminal-button"
          onClick={() => navigate('/')}
          style={{ marginLeft: 'auto', fontSize: '11px', padding: '0.25rem 0.75rem' }}
        >
          ← BACK
        </button>
      </div>

      <div style={{ padding: '2rem', maxWidth: '1400px', margin: '0 auto' }}>
        {/* Group Header */}
        <div style={{ marginBottom: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '0.5rem' }}>
            {group.icon && (
              <span style={{ fontSize: '32px' }}>{group.icon}</span>
            )}
            <h1 style={{ 
              fontSize: '28px', 
              fontWeight: 600, 
              color: group.color || 'var(--green-bright)',
              textShadow: group.color ? `0 0 10px ${group.color}` : 'none'
            }}>
              {group.name}
            </h1>
            {group.color && (
              <span 
                className="terminal-badge"
                style={{
                  backgroundColor: group.color,
                  color: 'var(--bg-primary)',
                  padding: '0.5rem 1rem',
                  fontSize: '12px'
                }}
              >
                {group.domainCount} DOMAINS
              </span>
            )}
          </div>
          {group.description && (
            <div style={{ color: 'var(--text-secondary)', fontSize: '14px', marginTop: '0.5rem' }}>
              {group.description}
            </div>
          )}
        </div>

        {/* Statistics Cards */}
        {stats && (
          <div className="terminal-grid" style={{ marginBottom: '2rem' }}>
            <div className="terminal-card">
              <div className="terminal-card-header">
                <div className="terminal-card-title">Total Domains</div>
              </div>
              <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--blue-bright)' }}>
                {stats.totalDomains}
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                {stats.enabledDomains} enabled
              </div>
            </div>

            <div className="terminal-card">
              <div className="terminal-card-header">
                <div className="terminal-card-title">Probe Runs</div>
              </div>
              <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--cyan)' }}>
                {stats.totalProbeRuns}
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                {stats.successfulRuns} success, {stats.failedRuns} failed
              </div>
            </div>

            <div className="terminal-card">
              <div className="terminal-card-header">
                <div className="terminal-card-title">Avg Latency</div>
              </div>
              <div style={{ fontSize: '32px', fontWeight: 600, color: 'var(--status-success)' }}>
                {stats.averageLatency}ms
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                Average response time
              </div>
            </div>

            <div className="terminal-card">
              <div className="terminal-card-header">
                <div className="terminal-card-title">Open Incidents</div>
              </div>
              <div style={{ fontSize: '32px', fontWeight: 600, color: stats.openIncidents > 0 ? 'var(--status-error)' : 'var(--status-success)' }}>
                {stats.openIncidents}
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                Active alerts
              </div>
            </div>
          </div>
        )}

        {/* Domains List */}
        <div className="terminal-card">
          <div className="terminal-card-header">
            <div className="terminal-card-title">Domains in Group</div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              {group.domains.length} domain{group.domains.length !== 1 ? 's' : ''}
            </div>
          </div>

          {group.domains.length === 0 ? (
            <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
              No domains in this group yet.
            </div>
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table className="terminal-table">
                <thead>
                  <tr>
                    <th>Domain</th>
                    <th>Status</th>
                    <th>Runs</th>
                    <th>Success Rate</th>
                    <th>Avg Latency</th>
                    <th>Last Check</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {group.domains.map((domain) => {
                    const domainStats = domainsStats.get(domain.id);
                    const successRate = domainStats && domainStats.totalRuns > 0
                      ? Math.round((domainStats.successfulRuns / domainStats.totalRuns) * 100)
                      : 0;

                    return (
                      <tr 
                        key={domain.id}
                        onClick={() => navigate(`/domain/${domain.id}`)}
                        style={{ cursor: 'pointer' }}
                      >
                        <td>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            {domain.icon && (
                              <span style={{ fontSize: '16px' }}>{domain.icon}</span>
                            )}
                            <span style={{ fontWeight: 600 }}>{domain.name}</span>
                          </div>
                        </td>
                        <td>
                          <span className={`terminal-badge ${domain.enabled ? 'success' : 'warning'}`}>
                            {domain.enabled ? 'ENABLED' : 'DISABLED'}
                          </span>
                        </td>
                        <td>{domainStats?.totalRuns || 0}</td>
                        <td>
                          <span style={{ color: successRate >= 90 ? 'var(--status-success)' : successRate >= 70 ? 'var(--status-warning)' : 'var(--status-error)' }}>
                            {successRate}%
                          </span>
                        </td>
                        <td>{domainStats?.avgLatency || 0}ms</td>
                        <td>
                          {domainStats?.lastRun ? new Date(domainStats.lastRun).toLocaleString() : 'Never'}
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            className="terminal-button"
                            onClick={(e) => {
                              e.stopPropagation();
                              navigate(`/domain/${domain.id}`);
                            }}
                            style={{ fontSize: '11px', padding: '0.25rem 0.5rem' }}
                          >
                            VIEW
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
