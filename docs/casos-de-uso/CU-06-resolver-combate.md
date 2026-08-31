# CU-06: resolver un combate táctico

## Objetivo

Resolver el encuentro entre ejércitos mediante decisiones tácticas legibles.

## Flujo principal

1. El juego presenta fuerzas, terreno y objetivo.
2. El jugador despliega sus formaciones.
3. Actúan primero todas las formaciones atacantes y después las defensoras.
4. Cada bando se ordena por tipo de tropa y posición visual.
5. El juego previsualiza la ruta como una cadena de puntos y marca solo el
   destino elegido con un `1`, además del coste real.
6. El jugador mueve, ataca, espera, defiende o usa una habilidad.
7. Esperar aplaza la formación al final de su bando; solo puede hacerse una
   vez por ronda; al volver puede mover, defender o atacar si tiene alcance.
8. Defender concede +2 hasta la siguiente acción de esa formación.
9. El héroe puede emitir una orden por ronda si dispone de mando.
10. La fase defensora se ejecuta automáticamente con el mismo orden y una
    pausa visible de preparación, resolución e impacto entre maniobras.
11. Moral, bajas y retirada se comprueban.
12. La batalla termina al cumplir su objetivo.
13. El resultado vuelve al mapa estratégico.

## Alternativas

- El jugador puede solicitar resolución automática.
- Puede retirarse si existe una ruta válida.
- Una rendición puede generar prisioneros y negociación.

## Criterios de aceptación

- El resultado depende de las mismas reglas en modo manual y automático.
- Las bajas y la fatiga persisten.
- La hueste derrotada desaparece aunque conserve supervivientes retirados;
  estos constan como dispersados y no abren otro combate desde la misma casilla.
- El héroe de la hueste derrotada recibe un desenlace persistente.
- El héroe principal derrotado queda herido y cautivo, nunca disponible para
  encabezar otra hueste mientras continúe así.
- La interfaz explica fase, orden, espera usada, defensa, daño, moral, ruta y
  coste de movimiento, sin numerar cada paso como si fuese otra activación.
- El parte reserva una altura estable y el tablero no se desplaza cuando sus
  mensajes pasan de una a dos líneas o viceversa.
- Una formación retirada o destruida desaparece del campo y no puede volver a
  actuar ni recibir ataques; la victoria bloquea inmediatamente nuevas órdenes.
- El combate no modifica el estado estratégico por fuera de sus eventos.
