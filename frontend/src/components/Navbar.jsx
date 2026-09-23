import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow-sm border-b border-calm-secondary/30">
      <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
        <Link to="/dashboard" className="flex items-center gap-2">
          <div className="w-9 h-9 rounded-full bg-calm-primary flex items-center justify-center text-white font-bold">
            M
          </div>
          <span className="text-xl font-semibold text-calm-dark">MindCare</span>
        </Link>

        {user && (
          <div className="flex items-center gap-4 text-sm">
            <Link to="/dashboard" className="hover:text-calm-primary">Accueil</Link>
            <Link to="/mood" className="hover:text-calm-primary">Humeur</Link>
            <Link to="/journal" className="hover:text-calm-primary">Journal</Link>
            <Link to="/doctors" className="hover:text-calm-primary">Docteurs</Link>
            <Link to="/conversations" className="hover:text-calm-primary">Messages</Link>
            <span className="text-calm-dark/70">|</span>
            <span className="font-medium">{user.fullName}</span>
            <button
              onClick={handleLogout}
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