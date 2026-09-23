import { useEffect, useState } from 'react';
import client from '../api/client';

export default function Mood() {
  const [moods, setMoods] = useState([]);
  const [score, setScore] = useState(5);
  const [note, setNote] = useState('');
  const [saving, setSaving] = useState(false);

  const load = () => client.get('/mood/mine').then((r) => setMoods(r.data));
  useEffect(() => { load(); }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    await client.post('/mood', { score, note });
    setNote('');
    setScore(5);
    await load();
    setSaving(false);
  };

  const getColor = (s) => {
    if (s <= 3) return 'text-red-500';
    if (s <= 6) return 'text-yellow-500';
    return 'text-green-500';
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Suivi de l'humeur</h1>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl p-6 shadow-sm space-y-4">
        <label className="block text-calm-dark font-medium">
          Comment vous sentez-vous ? <span className={`ml-2 font-bold ${getColor(score)}`}>{score}/10</span>
        </label>
        <input type="range" min="1" max="10" value={score}
          onChange={(e) => setScore(Number(e.target.value))}
          className="w-full accent-calm-primary"
        />
        <textarea placeholder="Une note (facultatif)..." value={note}
          onChange={(e) => setNote(e.target.value)}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
          rows={3}
        />
        <button type="submit" disabled={saving}
          className="px-6 py-3 rounded-xl bg-calm-primary text-white font-medium hover:bg-calm-dark transition disabled:opacity-50"
        >
          {saving ? 'Enregistrement...' : 'Enregistrer'}
        </button>
      </form>

      <div className="bg-white rounded-2xl p-6 shadow-sm">
        <h2 className="text-lg font-semibold text-calm-dark mb-4">Historique</h2>
        <ul className="divide-y divide-calm-secondary/20">
          {moods.map((m) => (
            <li key={m.id} className="py-3 flex justify-between items-center">
              <div>
                <p className="text-sm text-calm-dark/60">
                  {new Date(m.createdAt).toLocaleString('fr-FR')}
                </p>
                {m.note && <p className="text-calm-dark">{m.note}</p>}
              </div>
              <span className={`text-xl font-bold ${getColor(m.score)}`}>{m.score}/10</span>
            </li>
          ))}
          {moods.length === 0 && <li className="py-3 text-calm-dark/60 text-sm">Aucune entrée.</li>}
        </ul>
      </div>
    </div>
  );
}