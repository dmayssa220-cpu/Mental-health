import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

const STATUS_LABEL = {
  Pending: '⏳ En attente',
  Confirmed: '✅ Confirmé',
  Cancelled: '❌ Annulé',
  Completed: '✔️ Terminé',
  NoShow: '🚫 Absent',
};

export default function Appointments() {
  const { user } = useAuth();
  const [appointments, setAppointments] = useState([]);
  const [payingId, setPayingId] = useState(null);
  const isDoctor = user?.role === 'Doctor';

  const load = () => client.get('/appointments/mine').then((r) => setAppointments(r.data));
  useEffect(() => { load(); }, []);

  const cancel = async (id) => {
    if (!confirm('Annuler ce rendez-vous ?')) return;
    await client.delete(`/appointments/${id}`);
    load();
  };

  const updateStatus = async (id, status) => {
    await client.patch(`/appointments/${id}/status`, { status });
    load();
  };

  const pay = async (id) => {
    setPayingId(id);
    try {
      const response = await client.post(`/payments/appointment/${id}/checkout`);
      window.location.assign(response.data.url);
    } catch (error) {
      alert(error.response?.data?.message || 'Impossible de démarrer le paiement.');
    } finally {
      setPayingId(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-calm-dark">
          {isDoctor ? 'Mes rendez-vous' : 'Mes rendez-vous'}
        </h1>
        {!isDoctor && (
          <Link to="/doctors"
            className="px-4 py-2 rounded-xl bg-calm-primary text-white text-sm hover:bg-calm-dark transition">
            + Prendre un RDV
          </Link>
        )}
        {isDoctor && (
          <Link to="/doctor/schedule"
            className="px-4 py-2 rounded-xl bg-calm-primary text-white text-sm hover:bg-calm-dark transition">
            ⚙️ Gérer mes disponibilités
          </Link>
        )}
      </div>

      <div className="space-y-3">
        {appointments.map((a) => {
          const isPast = new Date(a.scheduledAt) < new Date();
          return (
            <div key={a.id} className="bg-white rounded-2xl p-5 shadow-sm">
              <div className="flex justify-between items-start">
                <div>
                  <p className="font-semibold text-calm-dark">
                    {isDoctor ? a.patient.fullName : `Dr. ${a.doctor.fullName}`}
                  </p>
                  <p className="text-sm text-calm-dark/60 mt-1">
                    {new Date(a.scheduledAt).toLocaleString('fr-FR')} · {a.durationMinutes} min
                  </p>
                  <span className="inline-block mt-2 px-2 py-1 rounded-full text-xs bg-calm-secondary/30 text-calm-dark">
                    {STATUS_LABEL[a.status] || a.status} · {a.type}
                  </span>
                  {a.reason && <p className="mt-2 text-sm text-calm-dark/70">Motif : {a.reason}</p>}
                </div>

                <div className="flex flex-col gap-2">
                  {a.status === 'Confirmed' && a.type === 'Video' && !isPast && (
                    <Link to={`/video/${a.id}`}
                      className="px-3 py-1.5 rounded-xl bg-green-500 text-white text-sm hover:bg-green-600 transition text-center">
                      🎥 Rejoindre
                    </Link>
                  )}
                  {!isDoctor && a.status === 'Confirmed' && !a.paymentId && (
                    <button onClick={() => pay(a.id)} disabled={payingId === a.id}
                      className="px-3 py-1.5 rounded-xl bg-calm-accent text-calm-dark text-sm hover:opacity-80 disabled:opacity-50">
                      {payingId === a.id ? 'Ouverture...' : 'Payer 50 €'}
                    </button>
                  )}
                  {isDoctor && a.status === 'Pending' && (
                    <>
                      <button onClick={() => updateStatus(a.id, 'Confirmed')}
                        className="px-3 py-1.5 rounded-xl bg-green-500 text-white text-sm hover:bg-green-600">
                        Confirmer
                      </button>
                      <button onClick={() => updateStatus(a.id, 'Cancelled')}
                        className="px-3 py-1.5 rounded-xl bg-red-500 text-white text-sm hover:bg-red-600">
                        Refuser
                      </button>
                    </>
                  )}
                  {!isDoctor && a.status === 'Pending' && (
                    <button onClick={() => cancel(a.id)}
                      className="px-3 py-1.5 rounded-xl bg-red-500 text-white text-sm hover:bg-red-600">
                      Annuler
                    </button>
                  )}
                </div>
              </div>
            </div>
          );
        })}
        {appointments.length === 0 && (
          <p className="text-calm-dark/60 text-center py-8">Aucun rendez-vous.</p>
        )}
      </div>
    </div>
  );
}