import { useState } from 'react';
import '../App.css';

const ICONS = [
  'globe', 'briefcase', 'graduation-cap', 'code', 'server', 'database',
  'shopping-cart', 'heart', 'star', 'bell', 'home', 'users',
  'shield', 'lock', 'unlock', 'check-circle', 'x-circle', 'alert-circle',
  'zap', 'cloud', 'cpu', 'wifi', 'smartphone', 'monitor'
];

interface IconPickerProps {
  value?: string;
  onChange: (icon: string | undefined) => void;
  disabled?: boolean;
}

export default function IconPicker({ value, onChange, disabled }: IconPickerProps) {
  const [showPicker, setShowPicker] = useState(false);

  const selectedIcon = value || 'globe';

  return (
    <div style={{ position: 'relative' }}>
      <button
        type="button"
        className="terminal-button"
        onClick={() => !disabled && setShowPicker(!showPicker)}
        disabled={disabled}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem',
          minWidth: '120px',
          justifyContent: 'center'
        }}
      >
        <span style={{ fontSize: '14px' }}>{selectedIcon}</span>
        <span style={{ fontSize: '10px' }}>▼</span>
      </button>

      {showPicker && (
        <>
          <div
            style={{
              position: 'fixed',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              zIndex: 1000
            }}
            onClick={() => setShowPicker(false)}
          />
          <div
            className="terminal-box"
            style={{
              position: 'absolute',
              top: '100%',
              left: 0,
              marginTop: '0.5rem',
              zIndex: 1001,
              padding: '0.5rem',
              maxHeight: '200px',
              overflowY: 'auto',
              display: 'grid',
              gridTemplateColumns: 'repeat(6, 1fr)',
              gap: '0.5rem',
              minWidth: '300px',
              maxWidth: '400px'
            }}
          >
            <button
              type="button"
              className={`terminal-button ${!value ? 'primary' : ''}`}
              onClick={() => {
                onChange(undefined);
                setShowPicker(false);
              }}
              style={{
                padding: '0.5rem',
                fontSize: '12px',
                textAlign: 'center'
              }}
            >
              None
            </button>
            {ICONS.map((icon) => (
              <button
                key={icon}
                type="button"
                className={`terminal-button ${value === icon ? 'primary' : ''}`}
                onClick={() => {
                  onChange(icon);
                  setShowPicker(false);
                }}
                style={{
                  padding: '0.5rem',
                  fontSize: '12px',
                  textAlign: 'center'
                }}
                title={icon}
              >
                {icon}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
