import { useState } from 'react';
import { groupsApi } from '../services/api';
import IconPicker from './IconPicker';
import type { CreateGroupRequest } from '../types';
import '../App.css';

interface GroupFormProps {
  onGroupAdded?: () => void;
  onCancel?: () => void;
}

const PRESET_COLORS = [
  '#00ff41', // Neon green
  '#00f5ff', // Cyan
  '#ff00ff', // Magenta
  '#ffaa00', // Orange
  '#aa00ff', // Purple
  '#ff0040', // Red
  '#00ff88', // Mint green
  '#ffaa00', // Amber
];

export default function GroupForm({ onGroupAdded, onCancel }: GroupFormProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [color, setColor] = useState<string | undefined>('#00ff41');
  const [icon, setIcon] = useState<string | undefined>(undefined);
  const [enabled, setEnabled] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!name.trim()) {
      setError('Group name is required');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const request: CreateGroupRequest = {
        name: name.trim(),
        description: description.trim() || undefined,
        color: color || undefined,
        icon: icon,
        enabled
      };

      await groupsApi.create(request);

      setName('');
      setDescription('');
      setColor('#00ff41');
      setIcon(undefined);
      setEnabled(true);
      onGroupAdded?.();
    } catch (err: any) {
      setError(err.response?.data?.error || err.response?.data?.message || err.message || 'Failed to create group');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div style={{ display: 'grid', gap: '1rem', marginBottom: '1rem' }}>
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
            Group Name *
          </label>
          <input
            type="text"
            className="terminal-input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g., Interview Prep, Career Websites"
            disabled={loading}
            required
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
            Description
          </label>
          <textarea
            className="terminal-input"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Optional description..."
            disabled={loading}
            rows={2}
            style={{ 
              resize: 'vertical',
              fontFamily: 'inherit',
              minHeight: '60px'
            }}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
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
              Color
            </label>
            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
              <input
                type="color"
                value={color || '#00ff41'}
                onChange={(e) => setColor(e.target.value)}
                disabled={loading}
                style={{
                  width: '50px',
                  height: '38px',
                  cursor: 'pointer',
                  border: '1px solid var(--border-color)',
                  borderRadius: '4px',
                  backgroundColor: 'var(--bg-primary)'
                }}
              />
              <input
                type="text"
                className="terminal-input"
                value={color || ''}
                onChange={(e) => setColor(e.target.value)}
                placeholder="#00ff41"
                disabled={loading}
                style={{ flex: 1 }}
              />
            </div>
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem', flexWrap: 'wrap' }}>
              {PRESET_COLORS.map((presetColor) => (
                <button
                  key={presetColor}
                  type="button"
                  onClick={() => setColor(presetColor)}
                  disabled={loading}
                  style={{
                    width: '30px',
                    height: '30px',
                    backgroundColor: presetColor,
                    border: color === presetColor ? '2px solid var(--text-primary)' : '1px solid var(--border-color)',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    boxShadow: color === presetColor ? `0 0 10px ${presetColor}` : 'none'
                  }}
                  title={presetColor}
                />
              ))}
            </div>
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
      </div>

      {error && (
        <div className="terminal-error" style={{ marginBottom: '1rem' }}>
          {error}
        </div>
      )}

      <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
        {onCancel && (
          <button
            type="button"
            className="terminal-button"
            onClick={onCancel}
            disabled={loading}
          >
            CANCEL
          </button>
        )}
        <button
          type="submit"
          className="terminal-button primary"
          disabled={loading || !name.trim()}
        >
          {loading ? 'CREATING...' : 'CREATE GROUP'}
        </button>
      </div>
    </form>
  );
}
