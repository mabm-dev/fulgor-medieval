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

1. Determinar iniciativa.
2. Activar una formación.
3. Mover, actuar o esperar.
4. Resolver daño, contraataque y efectos.
5. Comprobar moral y retirada.
6. Recuperar o consumir puntos de mando.
7. Terminar la ronda.

## Persistencia estratégica

Las bajas, heridas, prisioneros, fatiga y suministros continúan después de la
batalla. Retirarse a tiempo puede ser una decisión correcta.

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
