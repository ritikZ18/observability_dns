import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import DomainDetail from './pages/DomainDetail';
import GroupDetail from './pages/GroupDetail';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/domain/:id" element={<DomainDetail />} />
        <Route path="/group/:id" element={<GroupDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
