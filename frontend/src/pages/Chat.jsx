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
  const [aiOpen, setAiOpen] = useState(false);
  const [aiText, setAiText] = useState('');
  const [aiMessages, setAiMessages] = useState([]);
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

  return (
    <div className="bg-white rounded-2xl shadow-sm flex flex-col h-[75vh]">
      <div className="p-4 border-b border-calm-secondary/20">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-calm-dark">Conversation</h2>
          <button
            type="button"
            onClick={() => setAiOpen((open) => !open)}
            aria-expanded={aiOpen}
            aria-label={aiOpen ? 'Fermer le chatbot IA' : 'Ouvrir le chatbot IA'}
            className="px-3 py-1.5 rounded-xl bg-calm-accent text-calm-dark font-medium hover:opacity-80 transition"
          >
            🤖 IA
          </button>
        </div>
      </div>

      {aiOpen && (
        <section className="m-4 rounded-2xl border border-calm-accent/60 bg-calm-accent/10 p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold text-calm-dark">Assistant IA</h3>
            <button
              type="button"
              onClick={() => setAiOpen(false)}
              aria-label="Fermer le chatbot IA"
              className="text-calm-dark hover:opacity-60"
            >
              ×
            </button>
          </div>
          <div className="max-h-40 overflow-y-auto space-y-2 mb-3">
            {aiMessages.length === 0 && (
              <p className="text-sm text-calm-dark/70">Posez une question à l’assistant.</p>
            )}
            {aiMessages.map((message) => (
              <div
                key={message.id}
                className={`rounded-xl px-3 py-2 text-sm ${message.isFromAI ? 'bg-white text-calm-dark' : 'bg-calm-primary text-white ml-8'}`}
              >
                {message.content}
              </div>
            ))}
          </div>
          <form onSubmit={(e) => { e.preventDefault(); askAI(); }} className="flex gap-2">
            <input
              value={aiText}
              onChange={(e) => setAiText(e.target.value)}
              placeholder="Écrivez à l’assistant..."
              className="flex-1 px-3 py-2 rounded-xl border border-calm-secondary/40 focus:ring-2 focus:ring-calm-primary"
            />
            <button
              type="submit"
              disabled={aiLoading || !aiText.trim()}
              className="px-3 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition disabled:opacity-50"
            >
              {aiLoading ? '...' : 'Envoyer'}
            </button>
          </form>
        </section>
      )}

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
        <button type="submit"
          className="px-4 py-2 rounded-xl bg-calm-primary text-white hover:bg-calm-dark transition"
        >
          Envoyer
        </button>
      </form>
    </div>
  );
}