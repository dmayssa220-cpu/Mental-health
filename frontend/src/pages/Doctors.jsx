import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../api/client';

export default function Doctors() {
  const [doctors, setDoctors] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    client.get('/users/doctors').then((r) => setDoctors(r.data));
  }, []);

  const startConversation = async (doctorId) => {
    const res = await client.post(`/conversations/start/${doctorId}`);
    navigate(`/chat/${res.data.id}`);
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Nos docteurs</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {doctors.map((d) => (
          <div key={d.id} className="bg-white rounded-2xl p-6 shadow-sm">
            <Link to={`/appointments/new/${d.id}`}
  className="mt-2 w-full py-2 rounded-xl bg-calm-accent text-calm-dark hover:opacity-80 transition text-center block">
  📅 Prendre RDV
</Link>
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 rounded-full bg-calm-secondary/50 flex items-center justify-center text-2xl">
                👩‍⚕️
              </div>
              <div>
                <h3 className="font-semibold text-calm-dark">Dr. {d.fullName}</h3>
                <p className="text-sm text-calm-primary">{d.speciality || 'Généraliste'}</p>
              </div>
            </div>
            <p className="mt-3 text-sm text-calm-dark/60">{d.bio || 'Aucune bio disponible.'}</p>
            <button onClick={() => startConversation(d.id)}
              className="mt-4 w-full py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition"
            >
              Démarrer une conversation
            </button>
          </div>
        ))}
        {doctors.length === 0 && <p className="text-calm-dark/60">Aucun docteur inscrit.</p>}
      </div>
    </div>
  );
}