import {useState} from "react";
import ChatBox from "../components/ChatBox";

export default function AIModels(){

const[selected,setSelected]=useState("chat");

return(

<section className="profile-card">

<h2>AI Models</h2>

<div className="ai-layout">

<div className="ai-menu">

<button onClick={()=>setSelected("chat")}>
💬 AI Chat
</button>

<button onClick={()=>setSelected("voice")}>
🎤 Voice Chat
</button>

<button onClick={()=>setSelected("pdf")}>
📄 PDF Chat
</button>

<button onClick={()=>setSelected("vision")}>
👁 Vision AI
</button>

<button onClick={()=>setSelected("image")}>
🖼 Image Generator
</button>

<button onClick={()=>setSelected("code")}>
💻 Code Assistant
</button>

</div>

<div className="ai-content">

{selected==="chat" && <ChatBox/>}

{selected==="voice" &&
<h2>Voice Chat Coming Soon</h2>}

{selected==="pdf" &&
<h2>PDF Chat Coming Soon</h2>}

{selected==="vision" &&
<h2>Vision AI Coming Soon</h2>}

{selected==="image" &&
<h2>Image Generator Coming Soon</h2>}

{selected==="code" &&
<h2>Code Assistant Coming Soon</h2>}

</div>

</div>

</section>

);

}