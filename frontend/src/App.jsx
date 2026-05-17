import "./styles/global.css";
import AuthPage from "./pages/AuthPage";
import Dashboard from "./pages/Dashboard";

function App() {
  const token = localStorage.getItem("token");
  return token ? <Dashboard /> : <AuthPage />;
}
export default App;