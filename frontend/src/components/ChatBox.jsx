import { useState, useEffect, useRef } from "react";
import { getAuthHeaders } from "../services/authService";
import "../styles/chatbox.css";

export default function ChatBox() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [rateLimited, setRateLimited] = useState(false);
  const chatListRef = useRef(null);

  const handleSend = async () => {
    if (!input.trim()) return;
    setError("");
    setRateLimited(false);
    const question = input.trim();
    setInput("");
    setMessages((prev) => [...prev, { role: "user", text: question }]);
    setIsLoading(true);

    try {
      // Build a messages payload for session-aware chat
      const payloadMessages = [...messages, { role: "user", text: question }].map((m) => ({ role: m.role, content: m.text }));

      const response = await fetch("http://localhost:5243/api/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...getAuthHeaders(),
        },
        body: JSON.stringify({ messages: payloadMessages }),
      });

      const data = await response.json();
      if (!response.ok) throw new Error(data.error || "Chat request failed.");

      // Check if rate limited
      if (data.rateLimited) {
        setRateLimited(true);
      }
      
      setMessages((prev) => [...prev, { role: "assistant", text: data.reply }]);
    } catch (err) {
      setError(err.message || "Unable to send your message.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (chatListRef.current) {
      chatListRef.current.scrollTop = chatListRef.current.scrollHeight;
    }
  }, [messages]);

  return (
    <div className="chat-box">
      <div ref={chatListRef} className="chat-list">
        {messages.length === 0 ? (
          <div className="chat-empty-state">
            <p className="chat-empty-text">Hello! Ask me anything in AI Chat.</p>
            <button 
              className="chat-suggestion-btn"
              onClick={() => setInput("Ask me anything...")}
            >
              Ask anything to AI
            </button>
          </div>
        ) : (
          messages.map((message, index) => (
            <div
              key={index}
              className={`chat-message ${message.role === "user" ? "user" : "assistant"}`}
            >
              <span>{message.text}</span>
            </div>
          ))
        )}
      </div>
      {error && <div className="chat-error">{error}</div>}
      {rateLimited && <div className="chat-rate-limit-warning">⚠️ Rate limit reached. Please wait a few minutes before trying again.</div>}
      <div className="chat-input-row">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Ask AI Chat..."
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              handleSend();
            }
          }}
        />
        <button onClick={handleSend} disabled={isLoading}>
          {isLoading ? "Sending..." : "Send"}
        </button>
      </div>
    </div>
  );
}