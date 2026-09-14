import { LogIn } from "lucide-react";
import { type FormEvent, useState } from "react";

interface LoginPanelProps {
  isLoading: boolean;
  message: string;
  onLogin: (credentials: { email: string; password: string }) => Promise<void>;
}

/**
 * Renders the sign-in form used to establish an authenticated dashboard session.
 */
export function LoginPanel({ isLoading, message, onLogin }: LoginPanelProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  /**
   * Submits the entered credentials to the authentication workflow.
   */
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onLogin({ email, password });
  }

  return (
    <main className="auth-shell">
      <section className="auth-panel" aria-labelledby="login-title">
        <p className="eyebrow">Enterprise OCR Operations</p>
        <h1 id="login-title">KM AI Workflow OCR Platform</h1>
        <p className="auth-subtitle">Sign in to manage documents, OCR, AI extraction, and workflow tasks.</p>

        <form className="auth-form" onSubmit={handleSubmit}>
          <label>
            Email
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="username"
              required
            />
          </label>

          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
          </label>

          <button type="submit" disabled={isLoading || !email || !password} title="Sign in">
            <LogIn size={18} aria-hidden="true" />
            {isLoading ? "Signing in" : "Sign in"}
          </button>
        </form>

        <p className="auth-message" aria-live="polite">{message}</p>
      </section>
    </main>
  );
}
