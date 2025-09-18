interface ChatActionsProps {
  onClearChat: () => void;
  messagesCount: number;
}

export function ChatActions({
  onClearChat,
  messagesCount
}: ChatActionsProps) {
  return (
    <div className="flex gap-2">
      <button
        onClick={() => {
          if (window.confirm('Are you sure you want to clear all chat history? This cannot be undone.')) {
            onClearChat();
          }
        }}
        disabled={messagesCount === 0}
        className="px-3 py-1 text-sm bg-red-100 hover:bg-red-200 disabled:bg-gray-50 disabled:text-gray-400 text-red-700 rounded-lg transition-colors flex items-center gap-2"
        title="Clear all chat history"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
        </svg>
        Clear
      </button>
    </div>
  );
}