import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Forward /api calls to the ASP.NET Core API so the browser sees one origin (no CORS setup needed).
    proxy: {
      "/api": "http://localhost:8080",
    },
  },
});
