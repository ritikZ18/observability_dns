import type { WebsiteInfo } from '../types';

interface WebsiteInfoCardProps {
  info: WebsiteInfo;
}

export default function WebsiteInfoCard({ info }: WebsiteInfoCardProps) {
  return (
    <div style={{ padding: '1rem', background: '#f9f9f9', borderRadius: '8px', marginBottom: '1rem' }}>
      <h3 style={{ marginTop: 0 }}>Website Information: {info.domain}</h3>
      
      {info.ipAddresses.length > 0 && (
        <div style={{ marginBottom: '1rem' }}>
          <strong>IP Addresses:</strong>
          <ul style={{ margin: '0.5rem 0', paddingLeft: '1.5rem' }}>
            {info.ipAddresses.map((ip, idx) => (
              <li key={idx}>{ip}</li>
            ))}
          </ul>
        </div>
      )}

      {info.dnsRecords.length > 0 && (
        <div style={{ marginBottom: '1rem' }}>
          <strong>DNS Records:</strong>
          <table style={{ width: '100%', marginTop: '0.5rem', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#e0e0e0' }}>
                <th style={{ padding: '0.5rem', textAlign: 'left', border: '1px solid #ccc' }}>Type</th>
                <th style={{ padding: '0.5rem', textAlign: 'left', border: '1px solid #ccc' }}>Value</th>
                {info.dnsRecords.some(r => r.ttl) && (
                  <th style={{ padding: '0.5rem', textAlign: 'left', border: '1px solid #ccc' }}>TTL</th>
                )}
              </tr>
            </thead>
            <tbody>
              {info.dnsRecords.map((record, idx) => (
                <tr key={idx}>
                  <td style={{ padding: '0.5rem', border: '1px solid #ccc' }}>{record.type}</td>
                  <td style={{ padding: '0.5rem', border: '1px solid #ccc' }}>{record.value}</td>
                  {info.dnsRecords.some(r => r.ttl) && (
                    <td style={{ padding: '0.5rem', border: '1px solid #ccc' }}>{record.ttl || '-'}</td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {info.tlsInfo && (
        <div style={{ marginBottom: '1rem' }}>
          <strong>TLS Certificate:</strong>
          <div style={{ marginTop: '0.5rem', padding: '0.5rem', background: info.tlsInfo.isValid ? '#d4edda' : '#f8d7da', borderRadius: '4px' }}>
            <p style={{ margin: '0.25rem 0' }}>
              <strong>Status:</strong> {info.tlsInfo.isValid ? '✓ Valid' : '✗ Invalid'}
            </p>
            {info.tlsInfo.issuer && (
              <p style={{ margin: '0.25rem 0' }}><strong>Issuer:</strong> {info.tlsInfo.issuer}</p>
            )}
            {info.tlsInfo.subject && (
              <p style={{ margin: '0.25rem 0' }}><strong>Subject:</strong> {info.tlsInfo.subject}</p>
            )}
            {info.tlsInfo.daysUntilExpiry !== undefined && (
              <p style={{ margin: '0.25rem 0' }}>
                <strong>Days until expiry:</strong> {info.tlsInfo.daysUntilExpiry}
              </p>
            )}
            {info.tlsInfo.subjectAlternativeNames.length > 0 && (
              <div style={{ marginTop: '0.5rem' }}>
                <strong>SAN:</strong>
                <ul style={{ margin: '0.25rem 0', paddingLeft: '1.5rem' }}>
                  {info.tlsInfo.subjectAlternativeNames.map((san, idx) => (
                    <li key={idx}>{san}</li>
                  ))}
                </ul>
              </div>
            )}
            {info.tlsInfo.errorMessage && (
              <p style={{ margin: '0.25rem 0', color: '#721c24' }}>
                <strong>Error:</strong> {info.tlsInfo.errorMessage}
              </p>
            )}
          </div>
        </div>
      )}

      <p style={{ fontSize: '0.875rem', color: '#666', margin: 0 }}>
        Fetched at: {new Date(info.fetchedAt).toLocaleString()}
      </p>
    </div>
  );
}
