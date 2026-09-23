import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

export default function Dashboard() {
  const { user } = useAuth();
  const [moods, setMoods] = useState([]);
  const [stats, setStats] = useState({ average: 0, count: 0 });

  useEffect(() => {
    client.get('/mood/mine').then((r) => setMoods(r.data));
    client.get('/mood/stats').then((r) => setStats(r.data));
  }, []);

  const chartData = [...moods].reverse().map((m) => ({
    date: new Date(m.createdAt).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' }),
    score: m.score,
  }));

  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-r from-calm-primary to-calm-secondary text-white rounded-2xl p-8 shadow-md">
        <h1 className="text-3xl font-bold">Bonjour, {user?.fullName} 🌿</h1>
        <p className="mt-2 text-white/90">Prenez un moment pour vous aujourd'hui.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard label="Humeur moyenne" value={stats.average.toFixed(1)} suffix="/10" />
        <StatCard label="Entrées d'humeur" value={stats.count} />
        <StatCard label="Rôle" value={user?.role} />
      </div>

      <div className="bg-white rounded-2xl p-6 shadow-sm">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-lg font-semibold text-calm-dark">Évolution de l'humeur</h2>
          <Link to="/mood" className="text-sm text-calm-primary font-medium">Ajouter →</Link>
        </div>
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={chartData}>
              <XAxis dataKey="date" stroke="#A3C9D9" />
              <YAxis domain={[0, 10]} stroke="#A3C9D9" />
              <Tooltip />
              <Line type="monotone" dataKey="score" stroke="#6B8E9F" strokeWidth={3} dot={{ r: 5 }} />
            </LineChart>
          </ResponsiveContainer>
        ) : (
          <p className="text-calm-dark/60 text-sm">Aucune donnée. Ajoutez votre première humeur !</p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <QuickLink to="/journal" title="Journal" desc="Écrivez vos pensées" icon="📓" />
        <QuickLink to="/doctors" title="Docteurs" desc="Trouvez un professionnel" icon="👩‍⚕️" />
        <QuickLink to="/conversations" title="Messages" desc="Vos conversations" icon="💬" />
      </div>
    </div>
  );
}

function StatCard({ label, value, suffix = '' }) {
  return (
    <div className="bg-white rounded-2xl p-5 shadow-sm">
      <p className="text-calm-dark/60 text-sm">{label}</p>
      <p className="text-2xl font-bold text-calm-dark mt-1">{value}{suffix}</p>
    </div>
  );
}

function QuickLink({ to, title, desc, icon }) {
  return (
    <Link to={to}
      className="bg-white rounded-2xl p-5 shadow-sm hover:shadow-md transition flex items-center gap-4"
    >
      <div className="w-12 h-12 rounded-xl bg-calm-accent/40 flex items-center justify-center text-2xl">
        {icon}
      </div>
      <div>
        <p className="font-semibold text-calm-dark">{title}</p>
        <p className="text-sm text-calm-dark/60">{desc}</p>
      </div>
    </Link>
  );
}