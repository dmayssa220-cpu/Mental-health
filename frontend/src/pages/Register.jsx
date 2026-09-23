import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    fullName: '', email: '', password: '', role: 'Patient', speciality: ''
  });
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await register(form);
      navigate('/dashboard');
    } catch (err) {
      setError(err.response?.data?.message || "Erreur d'inscription");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-16 bg-white rounded-2xl shadow-lg p-8">
      <h1 className="text-2xl font-bold text-calm-dark mb-6">Créer un compte</h1>

      {error && <div className="bg-red-50 text-red-600 p-3 rounded-xl mb-4 text-sm">{error}</div>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <input type="text" placeholder="Nom complet" required
          value={form.fullName}
          onChange={(e) => setForm({ ...form, fullName: e.target.value })}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <input type="email" placeholder="Email" required
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <input type="password" placeholder="Mot de passe (min 6)" required minLength={6}
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <select
          value={form.role}
          onChange={(e) => setForm({ ...form, role: e.target.value })}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        >
          <option value="Patient">Patient</option>
          <option value="Doctor">Docteur</option>
        </select>
        {form.role === 'Doctor' && (
          <input type="text" placeholder="Spécialité (ex: Psychiatre)"
            value={form.speciality}
            onChange={(e) => setForm({ ...form, speciality: e.target.value })}
            className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
          />
        )}
        <button type="submit"
          className="w-full py-3 rounded-xl bg-calm-primary text-white font-medium hover:bg-calm-dark transition"
        >
          S'inscrire
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-calm-dark/60">
        Déjà un compte ? <Link to="/login" className="text-calm-primary font-medium">Connexion</Link>
      </p>
    </div>
  );
}