/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,jsx}"],
  theme: {
    extend: {
      colors: {
        'calm-primary': '#6B8E9F',
        'calm-secondary': '#A3C9D9',
        'calm-accent': '#D4E6B5',
        'calm-bg': '#F7FAFC',
        'calm-dark': '#2C3E50',
      },
      borderRadius: {
        'xl': '1rem',
        '2xl': '1.5rem',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}