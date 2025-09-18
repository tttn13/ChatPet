import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { ChatMessage as ChatMessageType } from '../types/chat';

interface ChatMessageProps {
  message: ChatMessageType;
}

export function ChatMessage({ message }: ChatMessageProps) {
  const [showThinking, setShowThinking] = useState(false);

  return (
    <div className={`flex ${message.isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div
        className={`max-w-3xl rounded-lg px-4 py-3 ${
          message.isUser
            ? 'bg-blue-500 text-white'
            : 'bg-gray-100 text-gray-800'
        }`}
      >
        <div className="text-sm font-semibold mb-1">
          {message.isUser ? 'You' : '🐾 Pet Assistant'}
        </div>
        <div className={`prose ${message.isUser ? 'prose-invert' : 'prose-gray'} max-w-none`}>
          {message.isUser ? (
            <div className="whitespace-pre-wrap">{message.message}</div>
          ) : (
            <ReactMarkdown 
              remarkPlugins={[remarkGfm]}
              components={{
                h1: ({children}) => <h1 className="text-2xl font-bold mt-4 mb-2">{children}</h1>,
                h2: ({children}) => <h2 className="text-xl font-bold mt-3 mb-2">{children}</h2>,
                h3: ({children}) => <h3 className="text-lg font-bold mt-2 mb-1">{children}</h3>,
                p: ({children}) => <p className="mb-2">{children}</p>,
                ul: ({children}) => <ul className="list-disc pl-5 mb-2">{children}</ul>,
                ol: ({children}) => <ol className="list-decimal pl-5 mb-2">{children}</ol>,
                li: ({children}) => <li className="mb-1">{children}</li>,
                blockquote: ({children}) => (
                  <blockquote className="border-l-4 border-gray-300 pl-4 italic my-2">
                    {children}
                  </blockquote>
                ),
                code: ({className, children}) => {
                  const isInline = !className;
                  return isInline ? (
                    <code className="bg-gray-200 px-1 py-0.5 rounded text-sm">{children}</code>
                  ) : (
                    <code className="block bg-gray-800 text-gray-100 p-3 rounded my-2 overflow-x-auto">
                      {children}
                    </code>
                  );
                },
                strong: ({children}) => <strong className="font-bold">{children}</strong>,
                em: ({children}) => <em className="italic">{children}</em>,
                hr: () => <hr className="my-4 border-gray-300" />,
                a: ({href, children}) => (
                  <a href={href} className="text-blue-600 hover:underline" target="_blank" rel="noopener noreferrer">
                    {children}
                  </a>
                ),
              }}
            >
              {message.response}
            </ReactMarkdown>
          )}
        </div>
        {!message.isUser && message.thinking && (
          <div className="mt-3">
            <button
              onClick={() => setShowThinking(!showThinking)}
              className="text-xs text-gray-500 hover:text-gray-700 underline"
            >
              {showThinking ? 'Hide' : 'Show'} Thinking Process
            </button>
            {showThinking && (
              <div className="mt-2 p-2 bg-gray-50 rounded text-sm text-gray-600 italic prose prose-sm max-w-none">
                <ReactMarkdown remarkPlugins={[remarkGfm]}>
                  {message.thinking}
                </ReactMarkdown>
              </div>
            )}
          </div>
        )}
        <div className="text-xs mt-2 opacity-70">
          {new Date(message.timestamp).toLocaleTimeString()}
        </div>
      </div>
    </div>
  );
}