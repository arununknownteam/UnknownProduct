import { useState, useEffect } from "react";
import { getAuthHeaders } from "../services/authService";

export default function Dashboard() {
  const [user, setUser] = useState(null);

  useEffect(() => {
    fetch("http://localhost:5243/api/user/profile", {
      headers: getAuthHeaders(),
    })
      .then((res) => res.json())
      .then((data) => setUser(data))
      .catch(() => setUser(null));
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    window.location.reload();
  };

  return (
    <div className="page">
      <div className="card">
        <h2>Dashboard</h2>
        {user ? (
          <>
            <p className="subtitle">Welcome, {user.userName}!</p>
            <p className="subtitle">{user.email}</p>
          </>
        ) : (
          <p className="subtitle">You are logged in!</p>
        )}
        <button onClick={handleLogout}>Logout</button>
      </div>
    </div>
  );
}