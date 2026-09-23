import { useEffect, useState } from 'react';
import client from '../api/client';

export default function Journal() {
  const [entries, setEntries] = useState([]);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [saving, setSaving] = useState(false);

  const load = () => client.get('/journal/mine').then((r) => setEntries(r.data));
  useEffect(() => { load(); }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    await client.post('/journal', { title, content });
    setTitle(''); setContent('');
    await load();
    setSaving(false);
  };

  const handleDelete = async (id) => {
    if (!confirm('Supprimer cette entrée ?')) return;
    await client.delete(`/journal/${id}`);
    load();
  };

  const badgeColor = (s) => ({
    positive: 'bg-green-100 text-green-700',
    negative: 'bg-red-100 text-red-700',
    neutral: 'bg-gray-100 text-gray-700',
  }[s] || 'bg-gray-100 text-gray-700');

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Journal personnel</h1>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl p-6 shadow-sm space-y-4">
        <input placeholder="Titre" required value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <textarea placeholder="Exprimez-vous librement..." required rows={6} value={content}
          onChange={(e) => setContent(e.target.value)}
          className="w-full px-4 py-3 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <button type="submit" disabled={saving}
          className="px-6 py-3 rounded-xl bg-calm-primary text-white font-medium hover:bg-calm-dark transition disabled:opacity-50"
        >
          {saving ? 'Analyse en cours...' : 'Publier'}
        </button>
      </form>

      <div className="space-y-4">
        {entries.map((e) => (
          <div key={e.id} className="bg-white rounded-2xl p-6 shadow-sm">
            <div className="flex justify-between items-start">
              <h3 className="font-semibold text-calm-dark">{e.title}</h3>
              <button onClick={() => handleDelete(e.id)}
                className="text-red-500 text-sm hover:underline">Supprimer</button>
            </div>
            <p className="text-sm text-calm-dark/60 mt-1">
              {new Date(e.createdAt).toLocaleString('fr-FR')}
            </p>
            <p className="mt-3 text-calm-dark whitespace-pre-wrap">{e.content}</p>
            {e.sentiment && (
              <span className={`inline-block mt-3 px-3 py-1 rounded-full text-xs font-medium ${badgeColor(e.sentiment)}`}>
                Sentiment : {e.sentiment}
              </span>
            )}
          </div>
        ))}
        {entries.length === 0 && (
          <p className="text-center text-calm-dark/60">Aucune entrée pour le moment.</p>
        )}
      </div>
    </div>
  );
}