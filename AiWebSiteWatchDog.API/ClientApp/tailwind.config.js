/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['system-ui', 'Inter', 'ui-sans-serif', 'sans-serif']
      }
    }
  },
  plugins: [require('@tailwindcss/forms')]
}
