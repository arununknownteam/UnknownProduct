import { useState, useEffect, useRef } from "react";
import { getAuthHeaders } from "../services/authService";

export default function ChatBox() {
  const [messages, setMessages] = useState([
    { role: "assistant", text: "Hello! Ask me anything in AI Chat." },
  ]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const chatListRef = useRef(null);

  const handleSend = async () => {
    if (!input.trim()) return;
    setError("");
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
        {messages.map((message, index) => (
          <div
            key={index}
            className={`chat-message ${message.role === "user" ? "user" : "assistant"}`}
          >
            <span>{message.text}</span>
          </div>
        ))}
      </div>
      {error && <div className="chat-error">{error}</div>}
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