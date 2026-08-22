# Visión del juego

## Propuesta

Fulgor Medieval es un videojuego 4X por turnos para navegador donde cada
decisión estratégica tiene una consecuencia visible sobre el territorio, la
población y los ejércitos.

## Pilares

1. **Reinos con identidad:** reglas comunes y asimetrías comprensibles.
2. **Territorio significativo:** terreno, distancia, suministro y recursos.
3. **Decisiones legibles:** el jugador entiende costes y consecuencias.
4. **Historia emergente:** IA, diplomacia y fronteras producen relatos.
5. **Épica contenida:** héroes poderosos sin anular la gestión del reino.

## Bucle 4X

```text
Explorar
  -> descubrir terreno, rutas y amenazas
Expandir
  -> fundar, conquistar, pactar o integrar
Explotar
  -> producir, construir, comerciar y abastecer
Competir
  -> diplomacia, presión, combate y rutas de victoria
  -> nuevo turno
```

## Primera vertical slice

- Mapa hexagonal de 24 × 16.
- Un reino jugable y un rival.
- Dos facciones independientes.
- Cinco recursos y seis edificios.
- Cuatro tipos de unidad por bando.
- Movimiento, suministro y niebla.
- Combate táctico básico y resolución automática equivalente.
- Guardar, cargar, victoria y derrota.
- Partida de 20 a 30 turnos.

No se ampliará el mapa ni el repertorio hasta que este recorrido completo sea
comprensible y divertido.

## Recursos iniciales

- Grano.
- Madera.
- Piedra.
- Oro.
- Mano de obra.

El agua comienza como propiedad del territorio. Hierro, caballos, sal y bienes
de lujo serán recursos estratégicos posteriores.

## Turno

1. Planificación y órdenes.
2. Movimiento.
3. Combates.
4. Producción y consumo.
5. Crecimiento y construcción.
6. Diplomacia e IA.
7. Eventos, visibilidad y guardado automático.

## Victorias

- Dominio.
- Prosperidad.
- Fulgor.

La vertical slice utilizará una sola condición para probar el bucle. Las otras
se incorporarán cuando economía, diplomacia y cultura estén completas.
