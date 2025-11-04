import axios from 'axios';

// Centralized Axios instance
// If VITE_BACKEND_URL is provided (e.g., in .env.local), use it.
// Otherwise use relative URLs so Vite proxy or same-origin will handle routing.
export const api = axios.create({
  baseURL: import.meta.env.VITE_BACKEND_URL || '',
  // withCredentials: false, // enable if you need cookies
  timeout: 15000,
});

export default api;
