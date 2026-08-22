/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        // Mejora 3: la paleta real del proyecto, auditada por frecuencia
        // de uso (no los 4 tonos que suponía el cuadernillo). Solo los
        // que se repiten de verdad como color de interfaz — las banderas
        // por reino (data/reinos.ts) y el terreno del mapa
        // (COLORES_TERRENO) son datos, no paleta de diseño, y se quedan
        // fuera a propósito. Sin uso en los .tsx todavía: migrar las
        // clases arbitrarias (`text-[#c8ad72]`, `border-[#c8ad72]/45`...)
        // a estos nombres queda pendiente, para no tocar la interfaz sin
        // poder verla.
        oro: '#c8ad72',
        'oro-claro': '#d8c68a',
        pergamino: '#e8d9ae',
        'oro-brillante': '#ffe6a3',
        'pergamino-palido': '#f3e5c0',
        dorado: '#d4af37',
        ambar: '#e8c96a',
        noche: '#070b10',
      },
    },
  },
  plugins: [],
}
