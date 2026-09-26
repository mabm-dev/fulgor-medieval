# Migración de Fulgor Medieval a Unity

Estado del repositorio: 27 de septiembre de 2026. Este directorio documenta el
proyecto Unity que contiene este mismo repositorio.

1. [Estado y evidencia](01-estado.md)
2. [Cambios respecto a la web](02-cambios-y-motivos.md)
3. [Estructura y fuente de verdad](03-estructura.md)
4. [Plan y criterios de cierre](04-plan.md)

La web TypeScript, conservada en la rama `legacy/web`, sirve de referencia de reglas. Unity reconstruye el juego en
C# y URP. El tablero actual es un prototipo técnico de 24×16 casillas; las
pruebas de paridad del generador no acreditan la paridad del juego completo.
La revisión visual y el rendimiento requieren comprobación en el editor y en
una build con gráficos.
