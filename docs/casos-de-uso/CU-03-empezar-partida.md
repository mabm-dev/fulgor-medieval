# CU-03 — Empezar partida

- **Actor:** jugador.
- **Precondiciones:** ninguna. Es la puerta de entrada al juego.

## Flujo principal

1. El jugador abre el juego y ve la **portada**: título, intro y "pulsa para entrar".
2. Accede al **menú principal**: "Construye tu reino" y "Cargar partida".
3. Si no existe ninguna partida, "Cargar partida" aparece **bloqueada**.
4. El jugador pulsa "Construye tu reino" y llega al **registro**.
5. Escribe el nombre de su dinastía y elige uno de los cinco reinos
   (cada ficha muestra color, bandera, héroe y tropa de élite según el lore).
6. Pulsa "Empezar partida".
7. El servidor valida los datos y crea, en una única transacción, el jugador
   y su reino con los recursos iniciales (100 de cada uno, turno 0).
8. El juego muestra la pantalla del reino recién creado.

## Flujos alternos

- **3a. Existe una partida:** "Cargar partida" está activa y lleva directamente
  a la pantalla del reino guardado.
- **5a. Nombre vacío o reino no elegido:** el formulario no se envía
  (validación del navegador) y el servidor lo vuelve a comprobar.
- **6a. Ya hay partida en curso:** el registro no se procesa; se muestra un
  aviso y el enlace de volver al menú. (De momento hay una única partida
  local; el multiusuario llega con el hito de login.)

## Postcondiciones

- Existe un jugador en `jugadores` y un reino ligado a él en `reinos`.
- El menú principal muestra "Cargar partida" desbloqueada.
