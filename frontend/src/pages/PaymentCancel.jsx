import { useNavigate } from 'react-router-dom';

export default function PaymentCancel() {
  const navigate = useNavigate();
  return (
    <div className="max-w-lg mx-auto mt-16 bg-white rounded-2xl p-8 shadow-lg text-center">
      <div className="text-5xl mb-4">⚠️</div>
      <h1 className="text-2xl font-bold text-calm-dark mb-2">Paiement annulé</h1>
      <p className="text-calm-dark/60 mb-6">Le paiement n'a pas été effectué. Vous pouvez réessayer.</p>
      <button onClick={() => navigate('/appointments')}
        className="px-6 py-3 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition">
        Retour
      </button>
    </div>
  );
}