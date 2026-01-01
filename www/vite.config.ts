import path from "node:path";
import react from "@vitejs/plugin-react-swc";
import { componentTagger } from "lovable-tagger";
import { defineConfig } from "vite";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  base: "/freetool/",
  server: {
    host: "::",
    port: 8081,
    proxy: {
      // Only proxy API routes, not static assets
      "/freetool/app": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/audit": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/dev": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/folder": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/resource": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/space": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/trash": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/user": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
      "/freetool/admin": {
        target: process.env.VITE_API_URL || "http://localhost:5002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/freetool/, ""),
      },
    },
  },
  plugins: [react(), mode === "development" && componentTagger()].filter(
    Boolean
  ),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
}));
