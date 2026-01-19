import { useState } from 'react';
import { domainsApi, websiteInfoApi } from '../services/api';
import type { CreateDomainRequest, WebsiteInfo } from '../types';

interface DomainFormProps {
  onDomainAdded?: () => void;
}

export default function DomainForm({ onDomainAdded }: DomainFormProps) {
  const [url, setUrl] = useState('');
  const [intervalMinutes, setIntervalMinutes] = useState(5);
  const [enabled, setEnabled] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [websiteInfo, setWebsiteInfo] = useState<WebsiteInfo | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const handlePreview = async () => {
    if (!url.trim()) return;

    setPreviewLoading(true);
    setError(null);
    try {
      const info = await websiteInfoApi.get(url.trim());
      setWebsiteInfo(info);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to fetch website info');
      setWebsiteInfo(null);
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) return;

    setLoading(true);
    setError(null);

    try {
      const request: CreateDomainRequest = {
        name: url.trim(),
        intervalMinutes,
        enabled,
      };

      await domainsApi.create(request);
      setUrl('');
      setWebsiteInfo(null);
      onDomainAdded?.();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to create domain');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ marginBottom: '2rem', padding: '1.5rem', border: '1px solid #ddd', borderRadius: '8px' }}>
      <h2 style={{ marginTop: 0 }}>Add Domain to Monitor</h2>
      
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
            Domain/URL:
          </label>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <input
              type="text"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="example.com or https://example.com"
              style={{ flex: 1, padding: '0.5rem', fontSize: '1rem' }}
            />
            <button
              type="button"
              onClick={handlePreview}
              disabled={previewLoading || !url.trim()}
              style={{ padding: '0.5rem 1rem', cursor: previewLoading ? 'wait' : 'pointer' }}
            >
              {previewLoading ? 'Loading...' : 'Preview'}
            </button>
          </div>
        </div>

        {websiteInfo && (
          <div style={{ marginBottom: '1rem', padding: '1rem', background: '#f0f0f0', borderRadius: '4px' }}>
            <h3 style={{ marginTop: 0 }}>Website Information</h3>
            <p><strong>Domain:</strong> {websiteInfo.domain}</p>
            {websiteInfo.ipAddresses.length > 0 && (
              <p><strong>IP Addresses:</strong> {websiteInfo.ipAddresses.join(', ')}</p>
            )}
            {websiteInfo.dnsRecords.length > 0 && (
              <div>
                <strong>DNS Records:</strong>
                <ul>
                  {websiteInfo.dnsRecords.map((record, idx) => (
                    <li key={idx}>{record.type}: {record.value}</li>
                  ))}
                </ul>
              </div>
            )}
            {websiteInfo.tlsInfo && (
              <div>
                <strong>TLS Certificate:</strong>
                <ul>
                  <li>Valid: {websiteInfo.tlsInfo.isValid ? 'Yes' : 'No'}</li>
                  {websiteInfo.tlsInfo.issuer && <li>Issuer: {websiteInfo.tlsInfo.issuer}</li>}
                  {websiteInfo.tlsInfo.daysUntilExpiry !== undefined && (
                    <li>Days until expiry: {websiteInfo.tlsInfo.daysUntilExpiry}</li>
                  )}
                  {websiteInfo.tlsInfo.errorMessage && (
                    <li style={{ color: 'red' }}>Error: {websiteInfo.tlsInfo.errorMessage}</li>
                  )}
                </ul>
              </div>
            )}
          </div>
        )}

        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 'bold' }}>
            Check Interval:
          </label>
          <select
            value={intervalMinutes}
            onChange={(e) => setIntervalMinutes(Number(e.target.value))}
            style={{ padding: '0.5rem', fontSize: '1rem' }}
          >
            <option value={1}>1 minute</option>
            <option value={5}>5 minutes</option>
            <option value={15}>15 minutes</option>
          </select>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <input
              type="checkbox"
              checked={enabled}
              onChange={(e) => setEnabled(e.target.checked)}
            />
            <span>Enabled</span>
          </label>
        </div>

        {error && (
          <div style={{ marginBottom: '1rem', padding: '0.5rem', background: '#fee', color: '#c00', borderRadius: '4px' }}>
            {error}
          </div>
        )}

        <button
          type="submit"
          disabled={loading || !url.trim()}
          style={{
            padding: '0.75rem 1.5rem',
            fontSize: '1rem',
            background: '#007bff',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: loading ? 'wait' : 'pointer'
          }}
        >
          {loading ? 'Adding...' : 'Add Domain'}
        </button>
      </form>
    </div>
  );
}
