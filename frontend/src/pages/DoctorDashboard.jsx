import { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import client from '../api/client';

export default function DoctorDashboard() {
  const [patients, setPatients] = useState([]);
  const [selected, setSelected] = useState(null);
  const [moods, setMoods] = useState([]);
  const [journals, setJournals] = useState([]);

  useEffect(() => {
    client.get('/doctors/patients').then((r) => setPatients(r.data));
  }, []);

  const openPatient = async (p) => {
    setSelected(p);
    const [moodsRes, journalsRes] = await Promise.all([
      client.get(`/doctors/patients/${p.id}/moods`),
      client.get(`/doctors/patients/${p.id}/journals`),
    ]);
    setMoods(moodsRes.data);
    setJournals(journalsRes.data);
  };

  const chartData = [...moods].reverse().map((m) => ({
    date: new Date(m.createdAt).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' }),
    score: m.score,
  }));

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Tableau de bord Docteur</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-2xl p-5 shadow-sm">
          <p className="text-calm-dark/60 text-sm">Patients suivis</p>
          <p className="text-2xl font-bold text-calm-dark mt-1">{patients.length}</p>
        </div>
        <div className="bg-white rounded-2xl p-5 shadow-sm">
          <p className="text-calm-dark/60 text-sm">Humeur moyenne (globale)</p>
          <p className="text-2xl font-bold text-calm-dark mt-1">
            {patients.length > 0
              ? (patients.reduce((s, p) => s + p.moodAverage, 0) / patients.length).toFixed(1)
              : '—'}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="bg-white rounded-2xl p-5 shadow-sm">
          <h2 className="font-semibold text-calm-dark mb-3">Mes patients</h2>
          <ul className="space-y-2">
            {patients.map((p) => (
              <li key={p.id}>
                <button
                  onClick={() => openPatient(p)}
                  className={`w-full text-left px-3 py-2 rounded-xl transition ${
                    selected?.id === p.id ? 'bg-calm-primary text-white' : 'hover:bg-calm-secondary/20'
                  }`}
                >
                  <p className="font-medium">{p.fullName}</p>
                  <p className="text-xs opacity-70">
                    Humeur moy. : {p.moodAverage.toFixed(1)}/10 · {p.moodCount} entrées
                  </p>
                </button>
              </li>
            ))}
            {patients.length === 0 && (
              <p className="text-calm-dark/60 text-sm">Aucun patient pour l'instant.</p>
            )}
          </ul>
        </div>

        <div className="lg:col-span-2 bg-white rounded-2xl p-5 shadow-sm">
          {!selected ? (
            <p className="text-calm-dark/60">Sélectionnez un patient pour voir ses données.</p>
          ) : (
            <>
              <h2 className="font-semibold text-calm-dark mb-3">Évolution de l'humeur — {selected.fullName}</h2>
              {chartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={220}>
                  <LineChart data={chartData}>
                    <XAxis dataKey="date" stroke="#A3C9D9" />
                    <YAxis domain={[0, 10]} stroke="#A3C9D9" />
                    <Tooltip />
                    <Line type="monotone" dataKey="score" stroke="#6B8E9F" strokeWidth={3} />
                  </LineChart>
                </ResponsiveContainer>
              ) : <p className="text-calm-dark/60 text-sm">Aucune donnée d'humeur.</p>}

              <h3 className="font-semibold text-calm-dark mt-6 mb-3">Dernières entrées du journal</h3>
              <ul className="space-y-2 max-h-60 overflow-y-auto">
                {journals.map((j) => (
                  <li key={j.id} className="border border-calm-secondary/20 rounded-xl p-3">
                    <p className="font-medium text-calm-dark text-sm">{j.title}</p>
                    <p className="text-xs text-calm-dark/60">{new Date(j.createdAt).toLocaleString('fr-FR')}</p>
                    <p className="text-sm text-calm-dark mt-1 line-clamp-2">{j.content}</p>
                  </li>
                ))}
                {journals.length === 0 && <p className="text-calm-dark/60 text-sm">Aucune entrée.</p>}
              </ul>
            </>
          )}
        </div>
      </div>
    </div>
  );
}