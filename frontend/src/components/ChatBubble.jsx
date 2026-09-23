export default function ChatBubble({ message, isMe }) {
  return (
    <div className={`flex ${isMe ? 'justify-end' : 'justify-start'}`}>
      <div className={`max-w-[75%] px-4 py-2 rounded-2xl ${
        message.isFromAI
          ? 'bg-calm-accent/40 text-calm-dark'
          : isMe
            ? 'bg-calm-primary text-white'
            : 'bg-calm-secondary/30 text-calm-dark'
      }`}>
        {message.isFromAI && (
          <p className="text-xs font-semibold mb-1">🤖 Assistant IA</p>
        )}
        <p className="whitespace-pre-wrap">{message.content}</p>
        <p className="text-xs opacity-70 mt-1">
          {new Date(message.sentAt).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
        </p>
      </div>
    </div>
  );
}