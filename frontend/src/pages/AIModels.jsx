import { useState } from "react";
import ChatBox from "../components/ChatBox";
import DocumentManager from "../components/DocumentManager";

const comingSoonFeatures = {
  voice: { icon: "🎤", title: "Voice Chat", description: "Voice-to-voice conversation is on the way." },
  vision: { icon: "👁", title: "Vision AI", description: "Upload images and ask questions about your visuals." },
  image: { icon: "🖼", title: "Image Generator", description: "Create images from text prompts with AI." },
  code: { icon: "💻", title: "Code Assistant", description: "AI-powered code completion and review." },
};

function ComingSoon({ feature }) {
  return (
    <div className="coming-soon-card">
      <div className="coming-soon-icon">{feature.icon}</div>
      <h3 className="coming-soon-title">{feature.title}</h3>
      <p className="coming-soon-description">{feature.description}</p>
      <span className="coming-soon-badge">Coming Soon</span>
    </div>
  );
}

export default function AIModels(){
  const [selected, setSelected] = useState("chat");
  const token = localStorage.getItem('token') || '';

  const menuItems = [
    { key: "chat",  icon: "💬", label: "AI Chat" },
    { key: "voice", icon: "🎤", label: "Voice Chat" },
    { key: "pdf",   icon: "📄", label: "PDF Chat" },
    { key: "vision", icon: "👁", label: "Vision AI" },
    { key: "image", icon: "🖼", label: "Image Generator" },
    { key: "code",  icon: "💻", label: "Code Assistant" },
  ];

 return(

<section className="profile-card ai-models-page">

  <header className="ai-models-header">
    <h2>AI Models</h2>
  </header>

  <div className="ai-layout">

    <nav className="ai-menu">

      <p className="ai-menu-caption">Choose an AI feature</p>

      {menuItems.map((item) => (
        <button
          key={item.key}
          className={selected === item.key ? "ai-menu-button active" : "ai-menu-button"}
          onClick={() => setSelected(item.key)}
        >
          <span className="ai-menu-button-icon">{item.icon}</span>
          <span className="ai-menu-button-label">{item.label}</span>
        </button>
      ))}

    </nav>

    <div className="ai-content">

      {selected === "chat" && <ChatBox/>}

      {selected === "pdf" && <DocumentManager token={token}/>}

      {selected === "voice" && <ComingSoon feature={comingSoonFeatures.voice}/>}

      {selected === "vision" && <ComingSoon feature={comingSoonFeatures.vision}/>}

      {selected === "image" && <ComingSoon feature={comingSoonFeatures.image}/>}

      {selected === "code" && <ComingSoon feature={comingSoonFeatures.code}/>}

    </div>

  </div>

</section>

);

}
