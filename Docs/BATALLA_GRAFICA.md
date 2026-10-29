# Campo de batalla gráfico

Los encuentros de campaña usan una pantalla opaca con el campo táctico en el centro,
las fuerzas propias a la izquierda y las rivales a la derecha. Recupera la organización
del prototipo web y representa el tablero mediante un diorama tridimensional.

- Pradera, arbolado y pedregal corresponden a los terrenos del motor de combate.
- Infantería, ballesteros y caballería tienen figuras y estandartes diferentes.
  Las figuras son representativas: la cifra de cada formación indica sus soldados reales.
- El azul identifica al jugador, incluso cuando defiende. El rojo identifica al rival.
- El oro identifica la formación activa, la selección y la ruta.
- Los paneles muestran soldados, moral, defensa e iniciativa. Consultar una formación
  no cambia el orden de actuación.

## Órdenes

1. Espera a que una formación propia tenga la iniciativa.
2. Pasa el cursor por las casillas azules para consultar ruta y coste.
3. Pulsa un destino y confirma **Mover aquí**, o selecciona un enemigo resaltado y
   confirma **Atacar formación**. Se validan ocupación, alcance y movimiento en el motor.
4. **Defender** añade la bonificación de defensa; **Esperar** aplaza la activación.
   **Combate automático** alterna el control automático de ambos bandos y su pausa.

La rueda o los botones acercan y alejan la cámara dentro de límites. **Encuadrar**
recupera la vista completa. Las selecciones y previsualizaciones no consumen azar,
movimiento ni recursos. La animación sigue la ruta que realmente aceptó el motor.

## Retirada, cierre y resultado

**Retirar hueste** solicita confirmación y muestra la previsión estratégica de retirada
o rendición. El rival se pausa mientras el aviso está abierto. **Continuar combatiendo**
cancela el aviso sin dar una orden.

Volver al mapa durante el combate conserva el encuentro pendiente. Al reanudarlo se
reconstruye el diorama a partir de las posiciones, heridas y moral actuales. Al terminar,
el resumen indica supervivientes y bajas de cada bando; **Aplicar y volver** entrega el
resultado a la campaña una sola vez. El cierre restaura la cámara y libera los recursos.

## Alcance

La presentación usa las reglas, semillas, formaciones y persistencia existentes; no
introduce un segundo simulador ni cambia el formato de guardado. El laboratorio
independiente conserva sus controles técnicos. Los soldados del diorama son figuras
procedurales estilizadas, sin un sistema de animación esquelética o combate individual.
No hay asedio, colisiones entre soldados ni despliegue manual previo al encuentro.
