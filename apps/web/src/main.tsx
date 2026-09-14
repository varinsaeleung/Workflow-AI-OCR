import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import "./styles.css";

/**
 * Mounts the React application into the document root.
 */
function bootstrap() {
  const root = document.getElementById("root");

  if (!root) {
    throw new Error("Root element was not found.");
  }

  createRoot(root).render(
    <StrictMode>
      <App />
    </StrictMode>
  );
}

bootstrap();
