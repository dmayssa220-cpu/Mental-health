import { Link, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { signalRService } from '../services/signalr';
import client from '../api/client';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [unread, setUnread] = useState(0);
  const homePath = user?.role === 'Doctor' ? '/doctor/dashboard' : '/dashboard';
  const isDoctor = user?.role === 'Doctor';

  useEffect(() => {
    if (!user) return;
    const loadUnread = () => {
      client.get('/conversations/unread-count')
        .then((res) => setUnread(res.data.count))
        .catch(() => setUnread(0));
    };
    loadUnread();
    const off = signalRService.on('notification', loadUnread);
    window.addEventListener('messages-read', loadUnread);
    return () => {
      off();
      window.removeEventListener('messages-read', loadUnread);
    };
  }, [user]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow-sm border-b border-calm-secondary/30">
      <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
        <Link to={homePath} className="flex items-center gap-2">
          <div className="w-9 h-9 rounded-full bg-calm-primary flex items-center justify-center text-white font-bold">M</div>
          <span className="text-xl font-semibold text-calm-dark">MindCare</span>
        </Link>

        {user && (
          <div className="flex items-center gap-4 text-sm">
            <Link to={homePath} className="hover:text-calm-primary">Accueil</Link>
            {!isDoctor && <>
              <Link to="/mood" className="hover:text-calm-primary">Humeur</Link>
              <Link to="/journal" className="hover:text-calm-primary">Journal</Link>
              <Link to="/doctors" className="hover:text-calm-primary">Docteurs</Link>
            </>}
            <Link to="/conversations" className="hover:text-calm-primary relative"
            >
              Messages
              {unread > 0 && (
                <span className="absolute -top-2 -right-3 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {unread > 9 ? '9+' : unread}
                </span>
              )}
            </Link>
            <span className="text-calm-dark/70">|</span>
            <span className="font-medium">{user.fullName}</span>
            <button onClick={handleLogout}
              className="px-3 py-1 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition"
            >
              Déconnexion
            </button>
          </div>
        )}
      </div>
    </nav>
  );
}