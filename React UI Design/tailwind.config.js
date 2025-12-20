
export default {
  content: [
  './index.html',
  './src/**/*.{js,ts,jsx,tsx}'
],
  theme: {
    extend: {
      colors: {
        space: {
          dark: '#0a0e27',
          darker: '#050816',
          light: '#1e293b'
        },
        nebula: {
          purple: '#6366f1',
          violet: '#8b5cf6',
          cyan: '#06b6d4',
          pink: '#ec4899'
        },
        star: {
          white: '#f8fafc',
          dim: '#94a3b8'
        }
      },
      fontFamily: {
        orbitron: ['Orbitron', 'sans-serif'],
        inter: ['Inter', 'sans-serif']
      },
      backdropBlur: {
        '2xl': '40px'
      },
      animation: {
        'float': 'float 6s ease-in-out infinite',
        'glow-pulse': 'glow-pulse 2s ease-in-out infinite'
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' }
        },
        'glow-pulse': {
          '0%, 100%': { opacity: '0.5' },
          '50%': { opacity: '1' }
        }
      }
    }
  },
  plugins: []
}
