import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import client from '../api/client';

export default function VideoCall() {
  const { appointmentId } = useParams();
  const navigate = useNavigate();
  const [session, setSession] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    client.post(`/video/appointment/${appointmentId}/session`)
      .then((res) => {
        setSession(res.data);
        client.post(`/video/session/${res.data.id}/start`).catch(() => {});
      })
      .catch((err) => setError(
        err.response?.data?.message ||
        'Impossible de rejoindre la session. Vérifiez que le rendez-vous est confirmé et que l’heure du rendez-vous est arrivée.'
      ))
      .finally(() => setLoading(false));
  }, [appointmentId]);

  const leave = async () => {
    if (session) {
      try { await client.post(`/video/session/${session.id}/end`); } catch {}
    }
    navigate('/appointments');
  };

  if (loading) return <div className="text-center py-20 text-calm-dark/60">Connexion à la salle...</div>;
  if (error) return (
    <div className="max-w-lg mx-auto bg-white rounded-2xl p-6 shadow-sm text-center">
      <p className="text-red-600 mb-4">{error}</p>
      <button onClick={() => navigate('/appointments')}
        className="px-4 py-2 rounded-xl bg-calm-primary text-white">Retour</button>
    </div>
  );

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-2xl p-4 shadow-sm flex items-center justify-between">
        <div>
          <h1 className="font-semibold text-calm-dark">
            Consultation — {session.appointment.doctor.fullName}
          </h1>
          <p className="text-xs text-calm-dark/60">
            {new Date(session.appointment.scheduledAt).toLocaleString('fr-FR')} · {session.appointment.durationMinutes} min
          </p>
        </div>
        <button onClick={leave}
          className="px-4 py-2 rounded-xl bg-red-500 text-white hover:bg-red-600 transition">
          Quitter
        </button>
      </div>

      <div className="bg-black rounded-2xl overflow-hidden shadow-lg" style={{ height: '70vh' }}>
        <iframe
          title="Jitsi Video"
          src={`${session.roomUrl}#userInfo.displayName=%22${encodeURIComponent('Participant')}%22`}
          allow="camera; microphone; fullscreen; display-capture; autoplay"
          style={{ width: '100%', height: '100%', border: 0 }}
        />
      </div>
    </div>
  );
}