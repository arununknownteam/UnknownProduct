import { useState } from "react";
import LoginForm from "../components/LoginForm";
import RegisterForm from "../components/RegisterForm";
import { loginUser, registerUser } from "../services/authService";
import "../styles/auth.css";

export default function AuthPage() {
  const [isLogin, setIsLogin] = useState(true);
  const [msg, setMsg] = useState("");

  const handleLogin = async (credentials) => {
    try {
      const data = await loginUser(credentials);
      localStorage.setItem("token", data.token);
      setMsg("Login successful");
      window.location.reload();
    } catch (err) {
      setMsg(err.message || "Server not running");
    }
  };

  const handleRegister = async (details) => {
    try {
      await registerUser(details);
      setMsg("Registered successfully. Please login.");
      setIsLogin(true);
    } catch (err) {
      setMsg(err.message || "Server not running");
    }
  };

  return (
    <div className="page">
      <div className="card">
        <h2>{isLogin ? "Welcome Back" : "Create Account"}</h2>
        <p className="subtitle">
          {isLogin ? "Login to continue" : "Register to get started"}
        </p>

        {isLogin ? (
          <LoginForm onLogin={handleLogin} />
        ) : (
          <RegisterForm onRegister={handleRegister} />
        )}

        <button className="switch" onClick={() => { setIsLogin(!isLogin); setMsg(""); }}>
          {isLogin ? "Create new account" : "Back to login"}
        </button>

        {msg && <p className="message">{msg}</p>}
      </div>
    </div>
  );
}