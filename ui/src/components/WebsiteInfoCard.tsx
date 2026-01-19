import type { WebsiteInfo } from '../types';
import '../App.css';

interface WebsiteInfoCardProps {
  info: WebsiteInfo;
}

export default function WebsiteInfoCard({ info }: WebsiteInfoCardProps) {
  return (
    <div className="terminal-card">
      <div className="terminal-card-header">
        <div className="terminal-card-title">Website Information Preview</div>
      </div>

      <div className="terminal-code" style={{ fontSize: '13px', lineHeight: '1.8' }}>
        <div style={{ marginBottom: '1rem' }}>
          <span style={{ color: 'var(--blue-bright)' }}>domain:</span>
          <span style={{ color: 'var(--text-primary)', marginLeft: '0.5rem' }}>{info.domain}</span>
        </div>

        {info.ipAddresses && info.ipAddresses.length > 0 && (
          <div style={{ marginBottom: '1rem' }}>
            <span style={{ color: 'var(--blue-bright)' }}>ip_addresses:</span>
            <div style={{ marginLeft: '1rem', marginTop: '0.25rem' }}>
              {info.ipAddresses.map((ip, idx) => (
                <div key={idx} style={{ color: 'var(--cyan)' }}>
                  {ip}
                </div>
              ))}
            </div>
          </div>
        )}

        {info.dnsRecords && info.dnsRecords.length > 0 && (
          <div style={{ marginBottom: '1rem' }}>
            <span style={{ color: 'var(--blue-bright)' }}>dns_records:</span>
            <div style={{ marginLeft: '1rem', marginTop: '0.25rem' }}>
              {info.dnsRecords.map((record, idx) => (
                <div key={idx} style={{ color: 'var(--text-primary)' }}>
                  <span style={{ color: 'var(--yellow)' }}>{record.type}</span>
                  {' '}
                  <span style={{ color: 'var(--cyan)' }}>{record.value}</span>
                  {record.ttl && (
                    <span style={{ color: 'var(--text-secondary)', marginLeft: '0.5rem' }}>
                      (TTL: {record.ttl}s)
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {info.tlsInfo && (
          <div style={{ marginBottom: '1rem' }}>
            <span style={{ color: 'var(--blue-bright)' }}>tls_info:</span>
            <div style={{ marginLeft: '1rem', marginTop: '0.25rem' }}>
              <div>
                <span style={{ color: 'var(--yellow)' }}>valid:</span>
                <span style={{ 
                  color: info.tlsInfo.isValid ? 'var(--status-success)' : 'var(--status-error)',
                  marginLeft: '0.5rem'
                }}>
                  {info.tlsInfo.isValid ? 'true' : 'false'}
                </span>
              </div>
              {info.tlsInfo.issuer && (
                <div>
                  <span style={{ color: 'var(--yellow)' }}>issuer:</span>
                  <span style={{ color: 'var(--text-primary)', marginLeft: '0.5rem' }}>
                    {info.tlsInfo.issuer}
                  </span>
                </div>
              )}
              {info.tlsInfo.notAfter && (
                <div>
                  <span style={{ color: 'var(--yellow)' }}>expires:</span>
                  <span style={{ color: 'var(--text-primary)', marginLeft: '0.5rem' }}>
                    {new Date(info.tlsInfo.notAfter).toLocaleString()}
                  </span>
                  {info.tlsInfo.daysUntilExpiry !== undefined && (
                    <span style={{ 
                      color: info.tlsInfo.daysUntilExpiry < 30 ? 'var(--status-error)' : 
                             info.tlsInfo.daysUntilExpiry < 60 ? 'var(--status-warning)' : 'var(--status-success)',
                      marginLeft: '0.5rem'
                    }}>
                      ({info.tlsInfo.daysUntilExpiry} days)
                    </span>
                  )}
                </div>
              )}
              {info.tlsInfo.subjectAlternativeNames && info.tlsInfo.subjectAlternativeNames.length > 0 && (
                <div>
                  <span style={{ color: 'var(--yellow)' }}>san:</span>
                  <div style={{ marginLeft: '1rem', marginTop: '0.25rem' }}>
                    {info.tlsInfo.subjectAlternativeNames.map((san, idx) => (
                      <div key={idx} style={{ color: 'var(--cyan)' }}>{san}</div>
                    ))}
                  </div>
                </div>
              )}
              {info.tlsInfo.errorMessage && (
                <div style={{ color: 'var(--status-error)', marginTop: '0.5rem' }}>
                  ERROR: {info.tlsInfo.errorMessage}
                </div>
              )}
            </div>
          </div>
        )}

        <div style={{ fontSize: '11px', color: 'var(--text-secondary)', marginTop: '1rem', borderTop: '1px solid var(--border-color)', paddingTop: '0.5rem' }}>
          <span style={{ color: 'var(--blue-bright)' }}>fetched_at:</span>
          <span style={{ marginLeft: '0.5rem' }}>{new Date(info.fetchedAt).toLocaleString()}</span>
        </div>
      </div>
    </div>
  );
}
