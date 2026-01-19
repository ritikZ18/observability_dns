import DomainForm from '../components/DomainForm';
import ObservabilityTable from '../components/ObservabilityTable';

export default function Dashboard() {
  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <h1 style={{ marginTop: 0 }}>DNS & TLS Observatory</h1>
      
      <DomainForm onDomainAdded={() => window.location.reload()} />
      
      <ObservabilityTable />
    </div>
  );
}
