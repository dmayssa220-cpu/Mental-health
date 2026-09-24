import { useEffect } from 'react';
import { signalRService } from '../services/signalr';
import { useAuth } from '../context/AuthContext';

export function useSignalR() {
  const { user } = useAuth();

  useEffect(() => {
    if (!user) return;
    signalRService.start();
    return () => {
     
    };
  }, [user]);

  return signalRService;
}