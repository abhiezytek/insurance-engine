/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        navy: '#004282',
        royal: '#007bff',
        danger: '#d32f2f',
      },
      boxShadow: {
        card: '0 8px 30px rgb(0,0,0,0.08)',
      },
    },
  },
  plugins: [require('@tailwindcss/forms')],
}

