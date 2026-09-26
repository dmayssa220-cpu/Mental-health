import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import client from '../api/client';

export default function BookAppointment() {
  const { doctorId } = useParams();
  const navigate = useNavigate();
  const [doctor, setDoctor] = useState(null);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [slots, setSlots] = useState([]);
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [type, setType] = useState('Video');
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    client.get('/users/doctors').then((r) => {
      setDoctor(r.data.find((d) => d.id === Number(doctorId)));
    }).catch(() => setError('Impossible de charger les informations du docteur.'));
  }, [doctorId]);

  useEffect(() => {
    setError('');
    client.get(`/appointments/doctors/${doctorId}/slots`, { params: { date } })
      .then((r) => { setSlots(r.data); setSelectedSlot(null); })
      .catch((e) => {
        setSlots([]);
        setSelectedSlot(null);
        setError(e.response?.data?.message || 'Impossible de charger les créneaux disponibles.');
      });
  }, [doctorId, date]);

  const book = async () => {
    if (!selectedSlot) { setError('Sélectionnez un créneau.'); return; }
    setLoading(true); setError('');
    try {
      await client.post('/appointments', {
        doctorId: Number(doctorId),
        scheduledAt: selectedSlot.start,
        durationMinutes: selectedSlot.duration,
        type,
        reason,
      });
      navigate('/appointments');
    } catch (e) {
      setError(e.response?.data?.message || 'Erreur');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">
        Prendre RDV {doctor ? `avec Dr. ${doctor.fullName}` : ''}
      </h1>

      {error && <div className="bg-red-50 text-red-600 p-3 rounded-xl">{error}</div>}

      <div className="bg-white rounded-2xl p-6 shadow-sm space-y-4">
        <div>
          <label className="text-sm text-calm-dark/70">Date</label>
          <input type="date" value={date} min={new Date().toISOString().slice(0, 10)}
            onChange={(e) => setDate(e.target.value)}
            className="w-full mt-1 px-3 py-2 rounded-xl border border-calm-secondary/40" />
        </div>

        <div>
          <label className="text-sm text-calm-dark/70">Créneaux disponibles</label>
          <div className="grid grid-cols-3 gap-2 mt-2">
            {slots.map((s, i) => (
              <button key={i} type="button"
                onClick={() => setSelectedSlot(s)}
                className={`px-3 py-2 rounded-xl text-sm border transition ${
                  selectedSlot?.start === s.start
                    ? 'bg-calm-primary text-white border-calm-primary'
                    : 'border-calm-secondary/40 hover:bg-calm-secondary/10'
                }`}>
                {new Date(s.start).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
              </button>
            ))}
            {slots.length === 0 && !error && (
              <p className="text-calm-dark/60 text-sm col-span-3">
                Aucun créneau disponible pour cette date. Le docteur doit d'abord définir ses disponibilités.
              </p>
            )}
          </div>
        </div>

        <div>
          <label className="text-sm text-calm-dark/70">Type de consultation</label>
          <select value={type} onChange={(e) => setType(e.target.value)}
            className="w-full mt-1 px-3 py-2 rounded-xl border border-calm-secondary/40">
            <option value="Video">🎥 Vidéo (50 €)</option>
            <option value="Chat">💬 Chat (30 €)</option>
            <option value="InPerson">🏥 En personne (60 €)</option>
          </select>
        </div>

        <div>
          <label className="text-sm text-calm-dark/70">Motif (optionnel)</label>
          <textarea value={reason} onChange={(e) => setReason(e.target.value)}
            rows={3}
            className="w-full mt-1 px-3 py-2 rounded-xl border border-calm-secondary/40" />
        </div>

        <button onClick={book} disabled={loading || !selectedSlot}
          className="w-full py-3 rounded-xl bg-calm-primary text-white font-medium hover:bg-calm-dark transition disabled:opacity-50">
          {loading ? 'Traitement...' : 'Confirmer la demande'}
        </button>
      </div>
    </div>
  );
}