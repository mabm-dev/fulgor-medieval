/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        // Mejora 3: la paleta real del proyecto, auditada sobre el codigo
        // (no los 4 tonos que suponia el cuadernillo: son 18, en tres
        // familias). Las banderas por reino (data/reinos.ts) y el terreno
        // del mapa (COLORES_TERRENO) se quedan fuera a proposito: son
        // datos, no paleta de diseno.
        //
        // Ningun valor cambio al centralizarlos: fue un renombrado puro,
        // para que el refactor no escondiera retoques visuales. Queda
        // anotado que `noche` tiene cuatro tonos casi indistinguibles
        // —los separa un 2 % de luminosidad— y que unificarlos seria una
        // mejora, pero de diseno, no de refactor.

        // Oros y pergaminos: la voz economica de la interfaz.
        oro: '#c8ad72',
        'oro-claro': '#d8c68a',
        'oro-viejo': '#e6c56f',
        'oro-brillante': '#ffe6a3',
        dorado: '#d4af37',
        ambar: '#e8c96a',
        pergamino: '#e8d9ae',
        'pergamino-palido': '#f3e5c0',

        // Aceros: el acento militar. Frio a proposito, para que las
        // huestes y el movimiento no se confundan con la economia.
        acero: {
          DEFAULT: '#5fb3d9',
          claro: '#8fd4f0',
        },

        // Aviso: hoy solo lo usa la hueste sin suministro.
        aviso: '#e0a458',

        // Noches: los fondos frios, nombrados por el sitio que ocupan.
        noche: {
          DEFAULT: '#070b10',
          fondo: '#02070b',
          mapa: '#05080d',
          tablero: '#091018',
        },

        // Brasas: los oscuros calidos, del boton de finalizar turno y del
        // degradado del retrato.
        brasa: {
          DEFAULT: '#151007',
          hover: '#241907',
          sombra: '#060502',
        },
      },
    },
  },
  plugins: [],
}
