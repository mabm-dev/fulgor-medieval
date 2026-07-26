export interface Reino {
  id: string
  nombre: string
  heroe: string
  tituloHeroe: string
  imagen: string
  descripcion: string
  tropas: string
  color: string
  colorNombre: string
}

export const REINOS: Reino[] = [
  {
    id: 'castilla',
    nombre: 'Castilla',
    heroe: 'Rodrigo',
    tituloHeroe: 'el Campeador',
    imagen: '/imagenes/heroe-castilla.jpg',
    descripcion:
      'Tierra de campos dorados y fortalezas de piedra. Sus huestes avanzan con la disciplina de quien jamás retrocede.',
    tropas: 'Caballería noble y concejil · Lanceros · Ballesteros',
    color: '#8C2B2B',
    colorNombre: 'Rojo castellano',
  },
  {
    id: 'leon',
    nombre: 'León',
    heroe: 'Urraca',
    tituloHeroe: 'la Leona',
    imagen: '/imagenes/heroe-leon.jpg',
    descripcion:
      'El reino que ruge bajo el estandarte del león. Murallas inquebrantables y soldados que luchan hasta el último aliento.',
    tropas: 'Infantería pesada · Milicias · Caballería',
    color: '#6B3FA0',
    colorNombre: 'Púrpura imperial',
  },
  {
    id: 'aragon',
    nombre: 'Aragón',
    heroe: 'Jaime',
    tituloHeroe: 'el Conquistador',
    imagen: '/imagenes/heroe-aragon.jpg',
    descripcion:
      'Del Pirineo al mar, su corona se forjó expandiendo fronteras. Veloz, ambiciosa y temida en campo abierto.',
    tropas: 'Almogávares · Ballesteros · Caballería feudal',
    color: '#D4AF37',
    colorNombre: 'Oro',
  },
  {
    id: 'navarra',
    nombre: 'Navarra',
    heroe: 'Aznar',
    tituloHeroe: 'de Roncal',
    imagen: '/imagenes/heroe-navarra.jpg',
    descripcion:
      'Señorío de montañas y desfiladeros. Sus gentes conocen cada piedra del camino y golpean donde nadie los espera.',
    tropas: 'Infanzones · Lanceros · Tiradores de montaña',
    color: '#A12B36',
    colorNombre: 'Rojo navarro',
  },
  {
    id: 'granada',
    nombre: 'Granada',
    heroe: 'Zahir',
    tituloHeroe: 'de Granada',
    imagen: '/imagenes/heroe-granada.jpg',
    descripcion:
      'El último reino del sur, guardián de jardines y palacios. Sus jinetes caen sobre el enemigo como el viento del desierto.',
    tropas: 'Jinetes ligeros · Lanceros · Arqueros andalusíes',
    color: '#209B6A',
    colorNombre: 'Verde nazarí',
  },
]
