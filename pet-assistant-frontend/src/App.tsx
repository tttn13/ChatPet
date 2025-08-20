import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ChatContainer } from './components/ChatContainer';
import { PetProfilePage } from './pages/PetProfilePage';

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<ChatContainer />} />
        <Route path="/profile" element={<PetProfilePage />} />
      </Routes>
    </Router>
  );
}

export default App;