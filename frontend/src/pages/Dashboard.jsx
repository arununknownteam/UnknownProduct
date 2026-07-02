import { useState, useEffect } from "react";
import { getAuthHeaders } from "../services/authService";

import Overview from "./Overview";
import AIModels from "./AIModels";
import Settings from "./Settings";
import Security from "./Security";
import Notifications from "./Notifications";

import "../styles/profile.css";

export default function Dashboard() {

  const [user, setUser] = useState(null);
  const [activePage, setActivePage] = useState("overview");

  const [darkMode, setDarkMode] = useState(() => {
    return localStorage.getItem("theme") === "dark";
  });

  useEffect(() => {

    fetch("http://localhost:5243/api/user/profile", {
      headers: getAuthHeaders(),
    })
      .then((res) => res.json())
      .then((data) => setUser(data))
      .catch(() => setUser(null));

  }, []);

  useEffect(() => {

    if (darkMode) {
      document.body.classList.add("dark");
      localStorage.setItem("theme", "dark");
    } else {
      document.body.classList.remove("dark");
      localStorage.setItem("theme", "light");
    }

  }, [darkMode]);

  const handleLogout = () => {
    localStorage.removeItem("token");
    window.location.reload();
  };

  return (

    <div className="profile-page">

      {/* Sidebar */}

      <aside className="profile-sidebar">

        <div className="profile-avatar-card">

          <div className="avatar">
            {user?.userName?.charAt(0)?.toUpperCase() || "U"}
          </div>

          <h3>{user?.userName || "User"}</h3>

          <p>{user?.email || "No Email"}</p>

        </div>

        <nav className="sidebar-nav">

          <button
            className={activePage === "overview" ? "nav-item active" : "nav-item"}
            onClick={() => setActivePage("overview")}
          >
            Overview
          </button>

          <button
            className={activePage === "ai" ? "nav-item active" : "nav-item"}
            onClick={() => setActivePage("ai")}
          >
            AI Models
          </button>

          <button
            className={activePage === "settings" ? "nav-item active" : "nav-item"}
            onClick={() => setActivePage("settings")}
          >
            Settings
          </button>

          <button
            className={activePage === "security" ? "nav-item active" : "nav-item"}
            onClick={() => setActivePage("security")}
          >
            Security
          </button>

          <button
            className={activePage === "notifications" ? "nav-item active" : "nav-item"}
            onClick={() => setActivePage("notifications")}
          >
            Notifications
          </button>

        </nav>

      </aside>

      {/* Main */}

      <main className="profile-main">

        <header className="profile-header">

          <div>

            <h1>
              Welcome back {user?.userName || "User"}
            </h1>

            <p className="subtitle">
              Manage your profile and AI tools.
            </p>

          </div>

          <div className="header-actions">

            <button
              className="theme-btn"
              onClick={() => setDarkMode(!darkMode)}
            >
              {darkMode ? "☀ Light" : "🌙 Dark"}
            </button>

            <button
              className="logout-button"
              onClick={handleLogout}
            >
              Logout
            </button>

          </div>

        </header>

        {activePage === "overview" && (
          <Overview user={user} />
        )}

        {activePage === "ai" && (
          <AIModels />
        )}

        {activePage === "settings" && (
          <Settings />
        )}

        {activePage === "security" && (
          <Security />
        )}

        {activePage === "notifications" && (
          <Notifications />
        )}

      </main>

    </div>

  );
}