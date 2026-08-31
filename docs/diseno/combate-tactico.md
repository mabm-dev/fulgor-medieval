# Combate táctico

## Objetivo

Ofrecer batallas por turnos sobre hexágonos donde importen composición,
terreno, moral y decisiones del comandante. La referencia está en los
clásicos de estrategia con ejércitos agrupados, pero reglas, nombres,
interfaz, recursos y contenido serán propios.

## Campo de batalla mínimo

- Rejilla hexagonal inicial de 13 × 9.
- Hasta seis formaciones por ejército.
- Despliegue previo limitado.
- Obstáculos, cobertura y casillas de altura.
- Zona de control y ataques de oportunidad definidos.

## Unidad

Cada formación posee:

```text
ataque
defensa
daño mínimo y máximo
salud por integrante
cantidad
movimiento
iniciativa
alcance
moral
disciplina
rasgos
```

La cantidad modifica las bajas infligidas, pero no el tamaño físico de la
formación en el tablero.

## Héroe

El héroe dirige desde fuera de la rejilla. Aporta atributos al ejército y
puede emitir una orden por ronda, limitada por puntos de mando.

Un capitán común podrá convertirse en héroe al cumplir determinadas
situaciones durante la campaña. Las condiciones, recompensas y límites de ese
ascenso se escribirán más adelante; por ahora queda como regla de progresión
reservada y no se asignan requisitos provisionales.

Órdenes iniciales:

- reagrupar;
- fortificar posición;
- carga coordinada;
- lluvia de proyectiles;
- marcha forzada;
- retirada fingida;
- inspirar;
- orden histórica exclusiva.

## Resolución de ronda

1. Actúa todo el bando atacante.
2. Actúa todo el bando defensor, controlado automáticamente.
3. Dentro de cada bando se agrupa por tipo: infantería, distancia y caballería.
4. Dentro de cada tipo se sigue la posición visual: superior antes que
   inferior y, a igual altura, izquierda antes que derecha.
5. La formación activa puede mover, esperar o defender; si tiene un objetivo
   válido también puede atacar.
6. Esperar la aplaza hasta después de la última formación de su bando. Solo se
   permite una vez por ronda y, al volver, puede mover, defender o atacar si
   posición y alcance permiten un objetivo válido.
7. Defender concede +2 a defensa hasta que esa formación vuelva a actuar.
8. Moral, bajas, retirada y puntos de mando se resuelven con cada acción.
9. Tras la última formación defensora comienza una nueva ronda atacante.

La interfaz presenta cada maniobra en tres momentos legibles: selección de la
formación activa, preparación de la orden y resolución. El bando automático
mantiene pausas deliberadas entre esos momentos. La ruta de movimiento se
dibuja como una cadena punteada y solo su destino lleva el número `1`: el
número representa una única orden, no los puntos de movimiento consumidos.

Una formación retirada o destruida queda fuera de la cola, deja libre su
casilla visual y no puede atacar ni ser objetivo. En cuanto un bando se queda
sin formaciones en liza, la sesión bloquea cualquier orden posterior.

## Persistencia estratégica

Las bajas, heridas, prisioneros, fatiga y suministros continúan después de la
batalla. Una hueste sin formaciones se disuelve: su capitán muere; el héroe
principal queda herido y cautivo del vencedor. El reino captor se conserva
para que v0.6 pueda resolver rescates, intercambios, pactos u otras concesiones.
Retirarse a tiempo puede ser una decisión correcta.

## Tipos de encuentro

- Batalla campal.
- Emboscada.
- Asedio.
- Defensa de paso.
- Ruptura de bloqueo.
- Escolta o ataque a convoy.
- Retirada.

## Resolución automática

La resolución automática utilizará el mismo motor determinista y las mismas
reglas. Podrá acelerar decisiones, pero no inventará un resultado mediante
una fórmula separada.

## Alcance de la primera versión

Cuatro unidades por bando, un héroe, tres terrenos, moral, ataque a distancia
y una condición de victoria. Asedios y habilidades avanzadas quedan fuera.
