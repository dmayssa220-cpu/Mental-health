import { useEffect } from 'react';
import { signalRService } from '../services/signalr';
import { useAuth } from '../context/AuthContext';

export function useSignalR() {
  const { user } = useAuth();

  useEffect(() => {
    if (!user) return;
    signalRService.start();
    const handlePageShow = () => signalRService.start();
    window.addEventListener('pageshow', handlePageShow);
    return () => {
      window.removeEventListener('pageshow', handlePageShow);
      signalRService.stop();
    };
  }, [user]);

  return signalRService;
}