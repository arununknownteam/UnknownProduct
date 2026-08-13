import { useState, useRef, useEffect } from 'react';
import axios from 'axios';
import '../styles/pdf-chat.css';

export default function DocumentChat({ document, token, onViewPage }) {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const sendMessage = async (e) => {
    e.preventDefault();
    
    if (!input.trim() || loading) return;

    const userMessage = input.trim();
    setInput('');
    setMessages(prev => [...prev, { role: 'user', content: userMessage }]);
    setLoading(true);
    setError('');

    try {
      const response = await axios.post(
        'http://localhost:5243/api/chat/pdf',
        {
          message: userMessage,
          documentId: document.id
        },
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        }
      );

      const aiMessage = {
        role: 'assistant',
        content: response.data.reply,
        citations: response.data.citations || []
      };

      setMessages(prev => [...prev, aiMessage]);
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to get response');
      setMessages(prev => [...prev, { 
        role: 'assistant', 
        content: 'Sorry, I encountered an error. Please try again.',
        error: true 
      }]);
    } finally {
      setLoading(false);
    }
  };

  const handleCitationClick = (citation) => {
    if (onViewPage && citation.page_number) {
      onViewPage(citation.page_number);
    }
  };

  const getFileIcon = () => {
    const ext = document.fileExtension?.toLowerCase() || '';
    switch (ext) {
      case '.pdf': return '📕';
      case '.docx':
      case '.doc': return '📘';
      case '.txt': return '📝';
      case '.csv': return '📊';
      case '.md':
      case '.markdown': return '📋';
      case '.html':
      case '.htm': return '🌐';
      default: return '📄';
    }
  };

  return (
    <div className="pdf-chat">
      <div className="chat-header">
        <h3>💬 Chat with: {getFileIcon()} {document.fileName}{document.fileExtension}</h3>
        <span className="document-info">
          {document.fileType} • {document.totalPages} sections • {document.totalChunks} chunks
        </span>
      </div>

      <div className="chat-messages">
        {messages.length === 0 && (
          <div className="welcome-message">
            <p>👋 Hi! I'm ready to answer questions about your document.</p>
            <p>Ask me anything about the content, and I'll provide answers with source citations.</p>
          </div>
        )}

        {messages.map((message, index) => (
          <div key={index} className={`message ${message.role}`}>
            <div className="message-content">
              <p>{message.content}</p>
              
              {message.citations && message.citations.length > 0 && (
                <div className="citations">
                  <p className="citations-title">📚 Sources:</p>
                  {message.citations.map((citation, idx) => (
                    <div 
                      key={idx} 
                      className="citation"
                      onClick={() => handleCitationClick(citation)}
                    >
                      <span className="citation-id">[{citation.citation_id}]</span>
                      <span className="citation-file">{citation.file_name}</span>
                      <span className="citation-page">Page {citation.page_number}</span>
                      <span className="citation-score">
                        {(citation.relevance_score * 100).toFixed(0)}% match
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}

        {loading && (
          <div className="message assistant">
            <div className="message-content">
              <div className="typing-indicator">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>
        )}

        {error && <div className="error-message">{error}</div>}
        <div ref={messagesEndRef} />
      </div>

      <form onSubmit={sendMessage} className="chat-input-form">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Ask a question about your document..."
          disabled={loading}
          className="chat-input"
        />
        <button 
          type="submit" 
          disabled={loading || !input.trim()}
          className="send-button"
        >
          Send
        </button>
      </form>
    </div>
  );
}