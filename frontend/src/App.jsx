import { useState } from "react";

function App() {
  const [id, setId] = useState("");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [msg, setMsg] = useState("");

  const saveUser = async () => {
    try {
      const res = await fetch("http://localhost:5243/api/user", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          id: parseInt(id),
          name: name,
          email: email,
        }),
      });

      if (res.ok) {
        setMsg("✅ User inserted successfully");
        setId("");
        setName("");
        setEmail("");
      } else {
        const text = await res.text();
        setMsg("❌ Error: " + text);
      }
    } catch (err) {
      setMsg("❌ Server not running");
    }
  };

  return (
    <div style={{ padding: 40, fontFamily: "Arial" }}>
      <h2>User Entry Form</h2>

      <div>
        <label>Id:</label><br/>
        <input
          value={id}
          onChange={(e) => setId(e.target.value)}
          placeholder="Enter Id"
        />
      </div>

      <br/>

      <div>
        <label>Name:</label><br/>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Enter Name"
        />
      </div>

      <br/>

      <div>
        <label>Email:</label><br/>
        <input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Enter Email"
        />
      </div>

      <br/>

      <button onClick={saveUser}>Save User</button>

      <p>{msg}</p>
    </div>
  );
}

export default App;
