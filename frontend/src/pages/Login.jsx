import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.message || 'Erreur de connexion');
    }
  };

  return (
    <div className="max-w-md mx-auto mt-16 bg-white rounded-2xl shadow-lg p-8">
      <h1 className="text-2xl font-bold text-calm-dark mb-2">Bon retour 👋</h1>
      <p className="text-calm-dark/60 mb-6">Connectez-vous pour continuer votre parcours.</p>

      {error && <div className="bg-red-50 text-red-600 p-3 rounded-xl mb-4 text-sm">{error}</div>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <input
          type="email" placeholder="Email" required
          value={email} onChange={(e) => setEmail(e.target.value)}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:outline-none focus:ring-2 focus:ring-calm-primary"
        />
        <input
          type="password" placeholder="Mot de passe" required
          value={password} onChange={(e) => setPassword(e.target.value)}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:outline-none focus:ring-2 focus:ring-calm-primary"
        />
        <button
          type="submit"
          className="w-full py-3 rounded-xl bg-calm-primary text-white font-medium hover:bg-calm-dark transition"
        >
          Se connecter
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-calm-dark/60">
        Pas de compte ? <Link to="/register" className="text-calm-primary font-medium">S'inscrire</Link>
      </p>
    </div>
  );
}