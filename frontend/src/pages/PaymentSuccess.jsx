import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import client from '../api/client';

export default function PaymentSuccess() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [payment, setPayment] = useState(null);

  useEffect(() => {
    const id = params.get('paymentId');
    if (id) client.get(`/payments/${id}`).then((r) => setPayment(r.data));
  }, [params]);

  return (
    <div className="max-w-lg mx-auto mt-16 bg-white rounded-2xl p-8 shadow-lg text-center">
      <div className="text-5xl mb-4">✅</div>
      <h1 className="text-2xl font-bold text-calm-dark mb-2">Paiement réussi</h1>
      <p className="text-calm-dark/60 mb-6">Votre rendez-vous est enregistré et sera confirmé par le docteur.</p>
      {payment && (
        <p className="text-sm text-calm-dark/70 mb-6">
          Montant : {(payment.amount).toFixed(2)} {payment.currency.toUpperCase()} · Statut : {payment.status}
        </p>
      )}
      <button onClick={() => navigate('/appointments')}
        className="px-6 py-3 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition">
        Voir mes rendez-vous
      </button>
    </div>
  );
}