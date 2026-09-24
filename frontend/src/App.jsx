import { Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Journal from './pages/Journal';
import Mood from './pages/Mood';
import Doctors from './pages/Doctors';
import Conversations from './pages/Conversations';
import Chat from './pages/Chat';
import DoctorDashboard from './pages/DoctorDashboard';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';
import { useSignalR } from './hooks/useSignalR';
import { useAuth } from './context/AuthContext';
import Appointments from './pages/Appointments';
import DoctorSchedule from './pages/DoctorSchedule';
import BookAppointment from './pages/BookAppointment';
import VideoCall from './pages/VideoCall';
import PaymentSuccess from './pages/PaymentSuccess';
import PaymentCancel from './pages/PaymentCancel';
function HomeRedirect() {
  const { user } = useAuth();
  return <Navigate to={user?.role === 'Doctor' ? '/doctor/dashboard' : '/dashboard'} replace />;
}

export default function App() {
  useSignalR();
  return (
    <div className="min-h-screen bg-calm-bg">
      <Navbar />
      <main className="max-w-6xl mx-auto px-4 py-6">
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/" element={<HomeRedirect />} />
          <Route path="/dashboard" element={<ProtectedRoute roles={['Patient']}><Dashboard /></ProtectedRoute>} />
          <Route path="/mood" element={<ProtectedRoute roles={['Patient']}><Mood /></ProtectedRoute>} />
          <Route path="/journal" element={<ProtectedRoute roles={['Patient']}><Journal /></ProtectedRoute>} />
          <Route path="/doctors" element={<ProtectedRoute roles={['Patient']}><Doctors /></ProtectedRoute>} />
          <Route path="/conversations" element={<ProtectedRoute><Conversations /></ProtectedRoute>} />
          <Route path="/chat/:id" element={<ProtectedRoute><Chat /></ProtectedRoute>} />
          <Route path="/doctor/dashboard" element={<ProtectedRoute roles={['Doctor']}><DoctorDashboard /></ProtectedRoute>} />
          <Route path="/appointments" element={<ProtectedRoute><Appointments /></ProtectedRoute>} />
<Route path="/appointments/new/:doctorId" element={<ProtectedRoute roles={['Patient']}><BookAppointment /></ProtectedRoute>} />
<Route path="/doctor/schedule" element={<ProtectedRoute roles={['Doctor']}><DoctorSchedule /></ProtectedRoute>} />
<Route path="/video/:appointmentId" element={<ProtectedRoute><VideoCall /></ProtectedRoute>} />
<Route path="/payment/success" element={<ProtectedRoute><PaymentSuccess /></ProtectedRoute>} />
<Route path="/payment/cancel" element={<ProtectedRoute><PaymentCancel /></ProtectedRoute>} />
        </Routes>
      </main>
    </div>
  );
}