/// <reference types="vitest" />
import { defineConfig } from 'vite';

// When deployed to GitHub Pages the app lives at /BionicLimbSimulation/
// VITE_BASE is injected by the deploy workflow; falls back to '/' for local dev.
const base = process.env.VITE_BASE ?? '/';

export default defineConfig({
  base,
  build: { target: 'es2020' },
  test: {
    environment: 'jsdom',
    globals: true,
  },
});
