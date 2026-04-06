import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Landing from './pages/Landing';
import TestSetup from './pages/TestSetup';
import Exam from './pages/Exam';
import Results from './pages/Results';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Landing />} />
        <Route path="/test/setup" element={<TestSetup />} />
        <Route path="/test/exam" element={<Exam />} />
        <Route path="/test/results" element={<Results />} />
      </Routes>
    </Router>
  );
}

export default App;
