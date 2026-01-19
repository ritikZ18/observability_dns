import { useState } from 'react';
import { apiClient } from '../services/api';
import '../App.css';

export default function BackupRestore() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleExport = async () => {
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await apiClient.get('/api/backup/export', {
        responseType: 'blob'
      });

      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = response.headers['content-disposition']
        ?.split('filename=')[1]?.replace(/"/g, '') || `backup-${new Date().toISOString()}.json`;
      
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      setSuccess('Backup exported successfully!');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to export backup');
    } finally {
      setLoading(false);
    }
  };

  const handleImport = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const text = await file.text();
      const backupData = JSON.parse(text);

      const clearExisting = confirm(
        'This will replace all existing domains and groups.\n\n' +
        'Check "Clear existing data" to remove all current data before importing.\n\n' +
        'Click OK to clear, Cancel to keep existing data.'
      );

      await apiClient.post('/api/backup/import', {
        backupData,
        clearExisting
      });

      setSuccess('Backup imported successfully! Refreshing page...');
      setTimeout(() => {
        window.location.reload();
      }, 2000);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to import backup');
    } finally {
      setLoading(false);
      // Reset file input
      event.target.value = '';
    }
  };

  return (
    <div className="terminal-card" style={{ marginTop: '2rem', borderTop: '2px solid var(--border-color)' }}>
      <div className="terminal-card-header">
        <div className="terminal-card-title">Backup & Restore</div>
        <div style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
          Export/Import domains and groups
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <div>
          <h3 style={{ fontSize: '13px', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--green-bright)' }}>
            Export Data
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
            Download all domains and groups as a JSON file. Save this file to restore your data later.
          </p>
          <button
            className="terminal-button primary"
            onClick={handleExport}
            disabled={loading}
            style={{ width: '100%' }}
          >
            {loading ? 'EXPORTING...' : '📥 EXPORT BACKUP'}
          </button>
        </div>

        <div>
          <h3 style={{ fontSize: '13px', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--cyan)' }}>
            Import Data
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
            Restore domains and groups from a previously exported JSON file.
          </p>
          <br />
          <label style={{ display: 'block' }}>
            <input
              type="file"
              accept=".json"
              onChange={handleImport}
              disabled={loading}
              style={{ display: 'none' }}
              id="import-file"
            />
            <button
              className="terminal-button"
              onClick={() => document.getElementById('import-file')?.click()}
              disabled={loading}
              style={{ width: '100%' }}
            >
              {loading ? 'IMPORTING...' : '📤 IMPORT BACKUP'}
            </button>
          </label>
        </div>
      </div>

      {error && (
        <div className="terminal-error" style={{ marginTop: '1rem' }}>
          {error}
        </div>
      )}

      {success && (
        <div className="terminal-success" style={{ marginTop: '1rem' }}>
          {success}
        </div>
      )}

      <div style={{ 
        marginTop: '1rem', 
        padding: '0.75rem', 
        backgroundColor: 'var(--bg-tertiary)', 
        border: '1px solid var(--border-color)',
        borderRadius: '4px',
        fontSize: '11px',
        color: 'var(--text-secondary)'
      }}>
        <strong>💡 Tip:</strong> Always export backup before restarting the database or making major changes. 
        The backup file contains all your domains, groups, and their configurations.
      </div>
    </div>
  );
}
