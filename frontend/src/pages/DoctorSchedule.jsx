import { useEffect, useState } from 'react';
import client from '../api/client';

const DAYS = ['Dimanche', 'Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi'];

export default function DoctorSchedule() {
  const [slots, setSlots] = useState([]);
  const [form, setForm] = useState({
    dayOfWeek: 1,
    startTime: '09:00',
    endTime: '17:00',
    slotDurationMinutes: 30,
  });

  const load = () => client.get('/appointments/availability').then((r) => setSlots(r.data));
  useEffect(() => { load(); }, []);

  const add = async (e) => {
    e.preventDefault();
    await client.post('/appointments/availability', {
      dayOfWeek: Number(form.dayOfWeek),
      startTime: form.startTime + ':00',
      endTime: form.endTime + ':00',
      slotDurationMinutes: Number(form.slotDurationMinutes),
      isActive: true,
    });
    load();
  };

  const remove = async (id) => {
    await client.delete(`/appointments/availability/${id}`);
    load();
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-calm-dark">Mes disponibilités</h1>

      <form onSubmit={add} className="bg-white rounded-2xl p-6 shadow-sm grid grid-cols-2 md:grid-cols-5 gap-3">
        <select value={form.dayOfWeek}
          onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}
          className="px-3 py-2 rounded-xl border border-calm-secondary/40">
          {DAYS.map((d, i) => <option key={i} value={i}>{d}</option>)}
        </select>
        <input type="time" value={form.startTime}
          onChange={(e) => setForm({ ...form, startTime: e.target.value })}
          className="px-3 py-2 rounded-xl border border-calm-secondary/40" />
        <input type="time" value={form.endTime}
          onChange={(e) => setForm({ ...form, endTime: e.target.value })}
          className="px-3 py-2 rounded-xl border border-calm-secondary/40" />
        <input type="number" min="10" max="120" step="5" value={form.slotDurationMinutes}
          onChange={(e) => setForm({ ...form, slotDurationMinutes: e.target.value })}
          className="px-3 py-2 rounded-xl border border-calm-secondary/40" />
        <button type="submit"
          className="px-4 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition">
          Ajouter
        </button>
      </form>

      <div className="bg-white rounded-2xl p-6 shadow-sm">
        <h2 className="font-semibold text-calm-dark mb-3">Créneaux définis</h2>
        <ul className="space-y-2">
          {slots.map((s) => (
            <li key={s.id} className="flex justify-between items-center px-3 py-2 rounded-xl bg-calm-secondary/10">
              <span className="text-sm text-calm-dark">
                <strong>{DAYS[s.dayOfWeek]}</strong> · {s.startTime} – {s.endTime} · {s.slotDurationMinutes} min
              </span>
              <button onClick={() => remove(s.id)}
                className="text-red-500 text-sm hover:underline">Supprimer</button>
            </li>
          ))}
          {slots.length === 0 && <p className="text-calm-dark/60 text-sm">Aucun créneau défini.</p>}
        </ul>
      </div>
    </div>
  );
}