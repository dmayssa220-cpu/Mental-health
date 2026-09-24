import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.listeners = new Map();
  }

  async start() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return this.connection;

    const token = localStorage.getItem('token');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => token || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Reconnexion : re-join les groupes si besoin (le ChatPage s'en occupe)
    this.connection.onreconnected(async () => {
      console.log('[SignalR] Reconnexion réussie');
      this.emit('reconnected', {});
    });

    this.connection.onreconnecting(() => {
      console.warn('[SignalR] Tentative de reconnexion...');
      this.emit('reconnecting', {});
    });

    this.connection.onclose(() => {
      console.warn('[SignalR] Connexion fermée');
      this.emit('closed', {});
    });

    // Enregistrer les événements serveur
    this.connection.on('ReceiveMessage', (msg) => this.emit('message', msg));
    this.connection.on('NewMessageNotification', (n) => this.emit('notification', n));
    this.connection.on('UserTyping', (data) => this.emit('typing', data));
    this.connection.on('UserStatusChanged', (data) => this.emit('status', data));
    this.connection.on('UserOnline', (data) => this.emit('online', data));
    this.connection.on('UserOffline', (data) => this.emit('offline', data));
    this.connection.on('MessagesRead', (data) => this.emit('read', data));

    try {
      await this.connection.start();
      console.log('[SignalR] Connecté');
    } catch (err) {
      console.error('[SignalR] Erreur de connexion', err);
      // Retry après 5s
      setTimeout(() => this.start(), 5000);
    }

    return this.connection;
  }

  async stop() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  // Gestion des listeners
  on(event, handler) {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set());
    this.listeners.get(event).add(handler);
    return () => this.off(event, handler);
  }

  off(event, handler) {
    this.listeners.get(event)?.delete(handler);
  }

  emit(event, payload) {
    this.listeners.get(event)?.forEach((h) => {
      try { h(payload); } catch (e) { console.error(e); }
    });
  }

  // Actions
  async joinConversation(conversationId) {
    await this.connection?.invoke('JoinConversation', conversationId);
  }

  async leaveConversation(conversationId) {
    await this.connection?.invoke('LeaveConversation', conversationId);
  }

  async sendMessage(conversationId, content) {
    await this.connection?.invoke('SendMessage', conversationId, content);
  }

  async sendTyping(conversationId, isTyping) {
    try {
      await this.connection?.invoke('Typing', conversationId, isTyping);
    } catch (e) { /* ignore */ }
  }

  async markAsRead(conversationId) {
    try {
      await this.connection?.invoke('MarkAsRead', conversationId);
    } catch (e) { /* ignore */ }
  }
}

export const signalRService = new SignalRService();