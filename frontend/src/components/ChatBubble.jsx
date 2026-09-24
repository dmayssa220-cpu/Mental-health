export default function ChatBubble({ message, isMe, onReact, onDownload }) {
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
        {message.attachments?.map((attachment) => (
          <button
            key={attachment.id}
            type="button"
            onClick={() => onDownload(attachment)}
            className="block mt-2 text-sm underline"
          >
            {attachment.originalName}
          </button>
        ))}
        {message.reactions?.length > 0 && (
          <div className="flex gap-1 mt-2">
            {message.reactions.map((reaction) => (
              <button key={reaction.emoji} type="button" onClick={() => onReact(message.id, reaction.emoji)}
                className="text-xs bg-white/70 rounded-full px-2 py-0.5">
                {reaction.emoji} {reaction.count}
              </button>
            ))}
          </div>
        )}
        <div className="flex gap-1 mt-2">
          {['👍', '❤️', '😂', '😢', '😮'].map((emoji) => (
            <button key={emoji} type="button" onClick={() => onReact(message.id, emoji)}
              className="text-xs opacity-60 hover:opacity-100" aria-label={`Réagir avec ${emoji}`}>
              {emoji}
            </button>
          ))}
        </div>
        {isMe && !message.isFromAI && (
          <p className="text-xs opacity-70 mt-1">{message.isRead ? 'Lu' : 'Envoyé'}</p>
        )}
        <p className="text-xs opacity-70 mt-1">
          {new Date(message.sentAt).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
        </p>
      </div>
    </div>
  );
}