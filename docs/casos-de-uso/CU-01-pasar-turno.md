# CU-01 — Pasar turno

- **Actor:** jugador.
- **Precondiciones:** existe su reino en la base de datos con sus recursos
  (madera, piedra, comida, agua) y el contador de turno.

## Flujo principal

1. El jugador pulsa el botón "Pasar turno".
2. El navegador envía la petición al servidor (fetch, sin recargar la página).
3. El servidor lee el reino desde la base de datos.
4. El servidor calcula la producción del turno y la suma a los recursos.
5. El servidor incrementa el contador de turno y guarda todo.
6. El servidor responde con los nuevos valores.
7. El navegador actualiza el panel de recursos con la respuesta.

## Flujos alternos

- **3a. El reino no existe:** el servidor responde con error y no cambia nada.
- **5a. Falla la base de datos:** el servidor responde con error; el jugador
  ve un mensaje y el turno no se consume.

## Postcondiciones

- El turno del reino ha avanzado en 1.
- Los recursos reflejan la producción aplicada.
