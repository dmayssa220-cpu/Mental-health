import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

export default function Conversations() {
  const { user } = useAuth();
  const [convs, setConvs] = useState([]);

  useEffect(() => {
    client.get('/conversations').then((r) => setConvs(r.data));
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Mes conversations</h1>
      <div className="space-y-3">
        {convs.filter((c) => c.patient.id !== c.doctor.id).map((c) => {
          const isPatient = user?.role === 'Patient';
          const other = isPatient ? c.doctor : c.patient;
          return (
            <Link key={c.id} to={`/chat/${c.id}`}
              className="block bg-white rounded-2xl p-4 shadow-sm hover:shadow-md transition"
            >
              <div className="flex justify-between items-center">
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded-full bg-calm-accent/40 flex items-center justify-center text-xl">
                    {isPatient ? '👩‍⚕️' : '🧑'}
                  </div>
                  <div>
                    <p className="font-semibold text-calm-dark">
                      {isPatient ? `Dr. ${other.fullName}` : other.fullName}
                    </p>
                    <p className="text-xs text-calm-dark/60">
                      Dernier message : {new Date(c.lastMessageAt).toLocaleString('fr-FR')}
                    </p>
                  </div>
                </div>
                {c.unreadCount > 0 && (
                  <span className="bg-red-500 text-white text-xs rounded-full min-w-6 h-6 px-1 flex items-center justify-center">
                    {c.unreadCount > 99 ? '99+' : c.unreadCount}
                  </span>
                )}
              </div>
            </Link>
          );
        })}
        {convs.length === 0 && <p className="text-calm-dark/60">Aucune conversation.</p>}
      </div>
    </div>
  );
}