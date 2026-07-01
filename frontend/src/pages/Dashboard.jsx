import { useState, useEffect } from "react";
import { getAuthHeaders } from "../services/authService";
import ChatBox from "../components/ChatBox";
import "../styles/profile.css";

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
    <div className="profile-page">
      <aside className="profile-sidebar">
        <div className="profile-avatar-card">
          <div className="avatar">{user?.userName?.[0]?.toUpperCase() || "U"}</div>
          <h3>{user?.userName || "User"}</h3>
          <p>{user?.email || "No email available"}</p>
        </div>

        <nav className="sidebar-nav">
          <button className="nav-item active">Overview</button>
          <button className="nav-item">Settings</button>
          <button className="nav-item">Security</button>
          <button className="nav-item">Notifications</button>
        </nav>
      </aside>

      <main className="profile-main">
        <header className="profile-header">
          <div>
            <p className="eyebrow">Profile</p>
            <h1>Welcome back, {user?.userName || "there"}</h1>
            <p className="subtitle">
              Manage your account, review your details, and update your settings.
            </p>
          </div>
          <button className="logout-button" onClick={handleLogout}>
            Logout
          </button>
        </header>

        <section className="profile-section">
          <div className="profile-card">
            <h2>Account details</h2>
            <div className="profile-details-grid">
              <div>
                <span className="label">Username</span>
                <p>{user?.userName || "-"}</p>
              </div>
              <div>
                <span className="label">Email</span>
                <p>{user?.email || "-"}</p>
              </div>
              <div>
                <span className="label">Joined</span>
                <p>{user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : "-"}</p>
              </div>
            </div>
          </div>
        </section>

        <section className="profile-section">
          <div className="profile-card">
            <div className="section-title-row">
              <div>
                <h2>AI Chat</h2>
                <p className="subtitle">
                  Talk with the assistant powered by the Python LLM integration.
                </p>
              </div>
            </div>
            <ChatBox />
          </div>
        </section>

        <section className="profile-section">
          <div className="profile-card">
            <h2>Quick actions</h2>
            <div className="action-list">
              <button>Edit profile</button>
              <button>Manage preferences</button>
              <button>Change password</button>
            </div>
          </div>
        </section>
      </main>

      <aside className="profile-secondary">
        <div className="profile-card small-card">
          <h3>Settings</h3>
          <p>Use these quick links to change your account setup and preferences.</p>
          <ul>
            <li>Account visibility</li>
            <li>Email notifications</li>
            <li>Password reset</li>
          </ul>
        </div>

        <div className="profile-card small-card">
          <h3>Profile summary</h3>
          <p>
            Logged in as <strong>{user?.userName || "User"}</strong>.
          </p>
          <p>Role: <strong>Member</strong></p>
          <p>Status: <strong>Active</strong></p>
        </div>
      </aside>
    </div>
  );

}