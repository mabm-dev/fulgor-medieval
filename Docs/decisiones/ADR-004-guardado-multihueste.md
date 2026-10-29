# ADR-004: guardado de campañas con varias huestes

Estado: aceptado para el prototipo de campaña.

## Contexto

El guardado v3 conserva una única hueste y el desarrollo urbano. Aplicarlo a un registro de varias huestes perdería tropas, ocupación del mapa y resultados de combate. Los edificios terminados aportan ingresos y mantenimiento que deben derivarse al cargar, sin duplicar bonificaciones.

## Decisión

Añadir una variante v4 explícita para campañas multihueste, manteniendo el lector y escritor anteriores para el modo simple. La captura contiene supervivientes, posición, movimiento, selección, victoria, historial de encuentros, recursos y ciudades.

Los atributos de tropas, facción y presupuesto de movimiento proceden del despliegue inicial del escenario; no se aceptan como reglas nuevas desde un archivo guardado. Una firma determinista incluye esos datos además del mapa y objetivo. Se conservan IDs de hueste y formación y se permiten subconjuntos por bajas. El sistema actual no incorpora reclutamiento ni refuerzos, por lo que la salud restaurada no puede superar la fuerza inicial.

La restauración construye registros nuevos y valida toda la campaña antes de publicar el estado. Los ingresos y el mantenimiento se recalculan desde el territorio y edificios propios. La instantánea no referencia colecciones mutables del juego activo.

Los encuentros tácticos abiertos bloquean la persistencia hasta que se implemente su estado completo. Se conservan los identificadores de encuentros resueltos para mantener la aplicación única de los resultados.

## Consecuencias

Los guardados antiguos mantienen su uso en modo simple. Los modos no se convierten silenciosamente. Los cambios de equilibrio o de despliegue que alteren la firma necesitarán una migración explícita. Reclutamiento, refuerzos, capturas de facción y batallas abiertas requerirán ampliar el contrato. Se mantiene la única ranura local existente; el almacenamiento en archivos y sus copias de recuperación quedan fuera de esta entrega.
