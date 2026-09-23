import { useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';
import ChatBubble from '../components/ChatBubble';

export default function Chat() {
  const { id } = useParams();
  const { user } = useAuth();
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState('');
  const [aiLoading, setAiLoading] = useState(false);
  const bottomRef = useRef(null);

  const load = () => client.get(`/conversations/${id}/messages`).then((r) => setMessages(r.data));
  useEffect(() => { load(); }, [id]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const send = async (e) => {
    e.preventDefault();
    if (!text.trim()) return;
    await client.post(`/conversations/${id}/messages`, { content: text });
    setText('');
    load();
  };

  const askAI = async () => {
    if (!text.trim()) return;
    setAiLoading(true);
    const res = await client.post('/ai/chat', { text });
    // Sauvegarde la réponse IA comme message local (non persisté pour l'instant)
    setMessages([...messages, {
      id: 'ai-' + Date.now(),
      content: res.data.response,
      isFromAI: true,
      sentAt: new Date().toISOString(),
    }]);
    setAiLoading(false);
  };

  return (
    <div className="bg-white rounded-2xl shadow-sm flex flex-col h-[75vh]">
      <div className="p-4 border-b border-calm-secondary/20">
        <h2 className="font-semibold text-calm-dark">Conversation</h2>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {messages.map((m) => (
          <ChatBubble key={m.id} message={m} isMe={m.senderId === user?.userId} />
        ))}
        <div ref={bottomRef} />
      </div>

      <form onSubmit={send} className="p-4 border-t border-calm-secondary/20 flex gap-2">
        <input value={text} onChange={(e) => setText(e.target.value)}
          placeholder="Écrivez un message..."
          className="flex-1 px-4 py-2 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
        />
        <button type="button" onClick={askAI} disabled={aiLoading}
          className="px-4 py-2 rounded-xl bg-calm-accent text-calm-dark font-medium hover:opacity-80 transition disabled:opacity-50"
        >
          {aiLoading ? '...' : '🤖 IA'}
        </button>
        <button type="submit"
          className="px-4 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition"
        >
          Envoyer
        </button>
      </form>
    </div>
  );
}