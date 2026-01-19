import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import DomainDetail from './pages/DomainDetail';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/domain/:id" element={<DomainDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
