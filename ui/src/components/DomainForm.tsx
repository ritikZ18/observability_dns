import { useState, useEffect } from 'react';
import { domainsApi, websiteInfoApi, groupsApi } from '../services/api';
import WebsiteInfoCard from './WebsiteInfoCard';
import IconPicker from './IconPicker';
import type { WebsiteInfo, Group } from '../types';
import '../App.css';

interface DomainFormProps {
  onDomainAdded?: () => void;
}

export default function DomainForm({ onDomainAdded }: DomainFormProps) {
  const [url, setUrl] = useState('');
  const [intervalMinutes, setIntervalMinutes] = useState(5);
  const [enabled, setEnabled] = useState(true);
  const [icon, setIcon] = useState<string | undefined>(undefined);
  const [groupId, setGroupId] = useState<string | undefined>(undefined);
  const [groups, setGroups] = useState<Group[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [preview, setPreview] = useState<WebsiteInfo | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  useEffect(() => {
    // Load groups
    groupsApi.getAll().then(setGroups).catch(() => {
      // Silently fail - groups are optional
    });
  }, []);

  const extractDomain = (input: string): string => {
    // Remove protocol, www, and trailing slashes
    let domain = input.trim().toLowerCase();
    domain = domain.replace(/^https?:\/\//, '');
    domain = domain.replace(/^www\./, '');
    domain = domain.replace(/\/.*$/, '');
    domain = domain.split(':')[0]; // Remove port if present
    return domain;
  };

  const handlePreview = async () => {
    const domain = extractDomain(url);
    if (!domain) {
      setError('Please enter a valid domain or URL');
      return;
    }

    setPreviewLoading(true);
    setError(null);
    setPreview(null);

    try {
      const info = await websiteInfoApi.get(domain);
      setPreview(info);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to fetch website info');
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const domain = extractDomain(url);
    if (!domain) {
      setError('Please enter a valid domain or URL');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await domainsApi.create({
        name: domain,
        intervalMinutes,
        enabled,
        icon,
        groupId
      });

      setUrl('');
      setIcon(undefined);
      setGroupId(undefined);
      setPreview(null);
      onDomainAdded?.();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to create domain');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <form onSubmit={handleSubmit} style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr auto auto', gap: '1rem', marginBottom: '1rem' }}>
          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '12px',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              textTransform: 'uppercase',
              letterSpacing: '0.5px'
            }}>
              Domain/URL
            </label>
            <input
              type="text"
              className="terminal-input"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://example.com or example.com"
              disabled={loading}
            />
          </div>

          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '12px',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              textTransform: 'uppercase',
              letterSpacing: '0.5px'
            }}>
              Interval
            </label>
            <select
              className="terminal-select"
              value={intervalMinutes}
              onChange={(e) => setIntervalMinutes(Number(e.target.value))}
              disabled={loading}
              style={{ width: '100%' }}
            >
              <option value={1}>1 minute</option>
              <option value={5}>5 minutes</option>
              <option value={15}>15 minutes</option>
            </select>
          </div>

          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '12px',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              textTransform: 'uppercase',
              letterSpacing: '0.5px'
            }}>
              Icon
            </label>
            <IconPicker
              value={icon}
              onChange={setIcon}
              disabled={loading}
            />
          </div>

          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '12px',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              textTransform: 'uppercase',
              letterSpacing: '0.5px'
            }}>
              Group
            </label>
            <select
              className="terminal-select"
              value={groupId || ''}
              onChange={(e) => setGroupId(e.target.value || undefined)}
              disabled={loading}
              style={{ width: '100%' }}
            >
              <option value="">None</option>
              {groups.filter(g => g.enabled).map((group) => (
                <option key={group.id} value={group.id}>
                  {group.name}
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button
              type="button"
              className="terminal-button"
              onClick={handlePreview}
              disabled={loading || previewLoading || !url.trim()}
              style={{ width: '100%' }}
            >
              {previewLoading ? 'LOADING...' : 'PREVIEW'}
            </button>
          </div>

          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button
              type="submit"
              className="terminal-button primary"
              disabled={loading || !url.trim()}
              style={{ width: '100%' }}
            >
              {loading ? 'ADDING...' : 'ADD DOMAIN'}
            </button>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <label style={{ 
            display: 'flex', 
            alignItems: 'center', 
            gap: '0.5rem',
            cursor: 'pointer',
            fontSize: '13px',
            color: 'var(--text-primary)'
          }}>
            <input
              type="checkbox"
              checked={enabled}
              onChange={(e) => setEnabled(e.target.checked)}
              disabled={loading}
              style={{ cursor: 'pointer' }}
            />
            <span>Enabled</span>
          </label>
        </div>
      </form>

      {error && (
        <div className="terminal-error" style={{ marginBottom: '1rem' }}>
          {error}
        </div>
      )}

      {preview && (
        <div style={{ marginTop: '1rem' }}>
          <WebsiteInfoCard info={preview} />
        </div>
      )}
    </div>
  );
}
