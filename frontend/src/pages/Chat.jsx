import { useEffect, useState, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';
import { signalRService } from '../services/signalr';
import ChatBubble from '../components/ChatBubble';

export default function Chat() {
  const { id } = useParams();
  const { user } = useAuth();
  const [messages, setMessages] = useState([]);
  const [conversation, setConversation] = useState(null);
  const [text, setText] = useState('');
  const [aiOpen, setAiOpen] = useState(false);
  const [aiText, setAiText] = useState('');
  const [aiMessages, setAiMessages] = useState([]);
  const [aiLoading, setAiLoading] = useState(false);
  const [isTyping, setIsTyping] = useState(false);
  const [otherOnline, setOtherOnline] = useState(false);
  const [sending, setSending] = useState(false);
  const bottomRef = useRef(null);
  const typingTimeoutRef = useRef(null);

  // --- Chargement initial REST ---
  const loadConversation = useCallback(() => {
    client.get(`/conversations/${id}`).then((r) => setConversation(r.data));
  }, [id]);

  const loadMessages = useCallback(() => {
    client.get(`/conversations/${id}/messages`).then((r) => setMessages(r.data));
  }, [id]);

  useEffect(() => {
    loadConversation();
    loadMessages();
  }, [loadConversation, loadMessages]);

  // --- SignalR : join + écoute ---
  useEffect(() => {
    if (!id) return;

    signalRService.start().then(() => {
      signalRService.joinConversation(Number(id));
      signalRService.markAsRead(Number(id));
    });

    const offMsg = signalRService.on('message', (msg) => {
      if (Number(msg.conversationId) !== Number(id)) return;
      setMessages((prev) => {
        // Éviter les doublons si le message a déjà été ajouté localement
        if (prev.some((m) => m.id === msg.id)) return prev;
        return [...prev, msg];
      });
    });

    const offTyping = signalRService.on('typing', (data) => {
      if (Number(data.conversationId) !== Number(id)) return;
      if (data.userId === user?.userId) return;
      setIsTyping(data.isTyping);
    });

    const offStatus = signalRService.on('status', (data) => {
      if (conversation?.otherUser?.id === data.userId) {
        setOtherOnline(data.isOnline);
      }
    });

    return () => {
      offMsg();
      offTyping();
      offStatus();
      signalRService.leaveConversation(Number(id));
    };
  }, [id, user?.userId, conversation?.otherUser?.id]);

  // Scroll auto
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  // --- Envoi de message via SignalR ---
  const send = async (e) => {
    e.preventDefault();
    if (!text.trim() || sending) return;
    setSending(true);
    try {
      await signalRService.sendMessage(Number(id), text.trim());
      setText('');
      signalRService.sendTyping(Number(id), false);
    } catch (err) {
      console.error(err);
      // Fallback REST
      await client.post(`/conversations/${id}/messages`, { content: text.trim() });
      setText('');
      loadMessages();
    } finally {
      setSending(false);
    }
  };

  // --- Indicateur de frappe ---
  const handleTextChange = (e) => {
    setText(e.target.value);
    signalRService.sendTyping(Number(id), true);
    clearTimeout(typingTimeoutRef.current);
    typingTimeoutRef.current = setTimeout(() => {
      signalRService.sendTyping(Number(id), false);
    }, 1500);
  };

  // --- Assistant IA (comme avant) ---
  const askAI = async () => {
    if (!aiText.trim()) return;
    setAiLoading(true);
    const question = aiText.trim();
    setAiText('');
    setAiMessages((current) => [
      ...current,
      { id: `user-${Date.now()}`, content: question, isFromAI: false },
    ]);

    try {
      const res = await client.post('/ai/chat', { text: question });
      setAiMessages((current) => [
        ...current,
        { id: `ai-${Date.now()}`, content: res.data.response, isFromAI: true },
      ]);
    } finally {
      setAiLoading(false);
    }
  };

  const other = conversation?.otherUser;

  return (
    <div className="bg-white rounded-2xl shadow-sm flex flex-col h-[78vh]">
      {/* Header avec statut en ligne */}
      <div className="p-4 border-b border-calm-secondary/20">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="relative">
              <div className="w-10 h-10 rounded-full bg-calm-secondary/40 flex items-center justify-center text-lg">
                {other?.role === 'Doctor' ? '👩‍⚕️' : '🧑'}
              </div>
              {otherOnline && (
                <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 rounded-full border-2 border-white" />
              )}
            </div>
            <div>
              <p className="font-semibold text-calm-dark">
                {other ? (other.role === 'Doctor' ? `Dr. ${other.fullName}` : other.fullName) : 'Chargement...'}
              </p>
              <p className="text-xs text-calm-dark/60">
                {isTyping ? '✍️ en train d’écrire...' : (otherOnline ? '🟢 En ligne' : '⚪ Hors ligne')}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => setAiOpen((open) => !open)}
            aria-expanded={aiOpen}
            className="px-3 py-1.5 rounded-xl bg-calm-accent text-calm-dark font-medium hover:opacity-80 transition"
          >
            🤖 IA
          </button>
        </div>
      </div>

      {/* Panneau IA (inchangé) */}
      {aiOpen && (
        <section className="m-4 rounded-2xl border border-calm-accent/60 bg-calm-accent/10 p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold text-calm-dark">Assistant IA</h3>
            <button type="button" onClick={() => setAiOpen(false)}
              className="text-calm-dark hover:opacity-60">×</button>
          </div>
          <div className="max-h-40 overflow-y-auto space-y-2 mb-3">
            {aiMessages.length === 0 && (
              <p className="text-sm text-calm-dark/70">Posez une question à l’assistant.</p>
            )}
            {aiMessages.map((message) => (
              <div key={message.id}
                className={`rounded-xl px-3 py-2 text-sm ${message.isFromAI ? 'bg-white text-calm-dark' : 'bg-calm-primary text-white ml-8'}`}
              >
                {message.content}
              </div>
            ))}
          </div>
          <form onSubmit={(e) => { e.preventDefault(); askAI(); }} className="flex gap-2">
            <input value={aiText} onChange={(e) => setAiText(e.target.value)}
              placeholder="Écrivez à l’assistant..."
              className="flex-1 px-3 py-2 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
            />
            <button type="submit" disabled={aiLoading || !aiText.trim()}
              className="px-3 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition disabled:opacity-50"
            >
              {aiLoading ? '...' : 'Envoyer'}
            </button>
          </form>
        </section>
      )}

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {messages.map((m) => (
          <ChatBubble key={m.id} message={m} isMe={m.senderId === user?.userId} />
        ))}
        {isTyping && (
          <div className="flex justify-start">
            <div className="bg-calm-secondary/30 rounded-2xl px-4 py-3 flex gap-1">
              <span className="w-2 h-2 bg-calm-dark/60 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-2 h-2 bg-calm-dark/60 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-2 h-2 bg-calm-dark/60 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      {/* Formulaire */}
      <form onSubmit={send} className="p-4 border-t border-calm-secondary/20 flex gap-2">
        <input
          value={text}
          onChange={handleTextChange}
          placeholder="Écrivez un message..."
          className="flex-1 px-4 py-2 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <button type="submit" disabled={sending || !text.trim()}
          className="px-4 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition disabled:opacity-50"
        >
          Envoyer
        </button>
      </form>
    </div>
  );
}