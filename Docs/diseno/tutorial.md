# Tutorial

## Principio

El tutorial es una campaña corta, no un manual ni una sucesión frágil de
ventanas. Enseña una mecánica, solicita una acción y muestra su consecuencia.

## Recorrido previsto

1. Conocer el reino y su ventaja.
2. Inspeccionar la capital.
3. Leer terreno y rendimientos.
4. Mover un explorador.
5. Descubrir niebla de guerra.
6. Finalizar el primer turno.
7. Resolver una carencia de comida.
8. Construir producción.
9. Reclutar y abastecer una unidad.
10. Recibir el aviso de una incursión.
11. Combatir, negociar o pagar tributo.
12. Crear un puesto fronterizo.
13. Contactar con otro reino.
14. Elegir una ruta de victoria.
15. Completar el escenario.

## Modelo de objetivo

Cada paso contiene:

```text
id
condición de inicio
explicación
acción esperada
condición de éxito
ayuda opcional
evento de finalización
```

El tutorial escucha eventos del juego. No conocerá coordenadas de botones ni
dependerá de un componente concreto.

## Experiencia

- Posibilidad de saltar, reiniciar y continuar.
- Bloqueo de acciones solo cuando sea indispensable.
- Explicación del porqué, no únicamente dónde pulsar.
- Cámara y resaltado como ayudas, no como reglas.
- Deshacer durante planificación; confirmar al finalizar el turno.
- Ayudas contextuales posteriores y códice consultable.
