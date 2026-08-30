# CU-06: resolver un combate táctico

## Objetivo

Resolver el encuentro entre ejércitos mediante decisiones tácticas legibles.

## Flujo principal

1. El juego presenta fuerzas, terreno y objetivo.
2. El jugador despliega sus formaciones.
3. Las unidades se activan por iniciativa.
4. El juego previsualiza la ruta y el coste sobre las casillas alcanzables.
5. El jugador mueve, ataca, espera o usa una habilidad.
6. El héroe puede emitir una orden por ronda si dispone de mando.
7. Moral, bajas y retirada se comprueban.
8. La batalla termina al cumplir su objetivo.
9. El resultado vuelve al mapa estratégico.

## Alternativas

- El jugador puede solicitar resolución automática.
- Puede retirarse si existe una ruta válida.
- Una rendición puede generar prisioneros y negociación.

## Criterios de aceptación

- El resultado depende de las mismas reglas en modo manual y automático.
- Las bajas y la fatiga persisten.
- Una hueste destruida desaparece y su héroe recibe un desenlace persistente.
- El héroe principal derrotado queda herido y cautivo, nunca disponible para
  encabezar otra hueste mientras continúe así.
- La interfaz explica modificadores de daño, moral, ruta y coste de movimiento.
- El combate no modifica el estado estratégico por fuera de sus eventos.
