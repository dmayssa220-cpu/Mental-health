import { createContext, useContext, useEffect, useState } from 'react';
import { signalRService } from '../services/signalr';
import { useAuth } from './AuthContext';

const NotificationContext = createContext(null);

export function NotificationProvider({ children }) {
  const { user } = useAuth();
  const [toasts, setToasts] = useState([]);

  useEffect(() => {
    if (!user) return;

    const offNotif = signalRService.on('notification', (n) => {
      const id = Date.now();
      setToasts((prev) => [...prev, { id, ...n }]);
      // Auto-dismiss après 5s
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, 5000);
    });

    return () => offNotif();
  }, [user]);

  const dismiss = (id) => setToasts((prev) => prev.filter((t) => t.id !== id));

  return (
    <NotificationContext.Provider value={{ toasts, dismiss }}>
      {children}
      {/* Container de toasts */}
      <div className="fixed bottom-4 right-4 space-y-2 z-50">
        {toasts.map((t) => (
          <div key={t.id}
            className="bg-white border-l-4 border-calm-primary shadow-lg rounded-xl p-4 max-w-sm animate-slide-in"
          >
            <div className="flex justify-between items-start">
              <div>
                <p className="font-semibold text-calm-dark text-sm">💬 {t.from}</p>
                <p className="text-calm-dark/70 text-sm mt-1">{t.preview}</p>
              </div>
              <button onClick={() => dismiss(t.id)} className="text-calm-dark/40 hover:text-calm-dark">×</button>
            </div>
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
}

export const useNotifications = () => useContext(NotificationContext);