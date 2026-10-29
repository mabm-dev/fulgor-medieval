# Gestión y economía de la ciudad

Los edificios construidos tienen efectos reales en las reservas del reino. La vista de ciudad consulta el estado de campaña; abrirla o cerrarla no genera recursos ni avanza el tiempo.

## Actividad y mantenimiento

| Edificio terminado | Ingreso por turno | Mantenimiento por turno |
|---|---|---|
| Mercado | 8 oro | 2 grano |
| Pozo | 6 grano, por abastecimiento de huertos | 1 oro |
| Almacén | 4 madera y 3 piedra, por aprovechamiento de suministros | 2 oro |
| Cada vivienda adicional | 3 mano de obra | 1 grano |

Son valores iniciales de equilibrio, definidos en `CityProjects`, junto al coste de obra y su duración. La fortaleza y las dos viviendas iniciales no añaden mantenimiento ni bonificaciones adicionales. Los vecinos y animales visibles siguen siendo ambientación; no constituyen un censo de población ni un sistema de consumo individual. El almacén mejora el rendimiento de materiales y no establece todavía un límite de almacenamiento.

Las rentas del territorio se reparten por la fortaleza más cercana y su propietario, con desempate por ID. Con una sola fortaleza propia se conserva el ingreso global anterior; con varios propietarios solo se cobra el territorio correspondiente. Se suman las aportaciones de edificios terminados en ciudades propias. La [conquista](CONQUISTA_Y_PROPIEDAD.md) transfiere distrito, edificios y mantenimiento al nuevo dueño.

## Cierre de turno

1. Se añaden los ingresos del turno que termina.
2. Se paga el mantenimiento de los edificios que estaban activos durante ese turno.
3. Se completan las obras que llegan a su turno de finalización.
4. Se recalculan ingresos y mantenimiento para el siguiente cierre.

Por ejemplo, un mercado iniciado en el turno 2 termina al entrar en el turno 4. Los recursos del cierre 3 → 4 todavía no incluyen el mercado. Su primera aportación y mantenimiento se aplican en el cierre 4 → 5. No hay cobro retroactivo.

El mantenimiento puede usar tanto los ingresos como las reservas existentes. Si no alcanzan, cada recurso baja como mínimo a cero; no se acumula deuda. Hacienda muestra el mantenimiento sin cubrir. Todavía no hay penalizaciones adicionales, cierre de edificios o deuda por impago.

El coste de construcción se paga una sola vez al empezar la obra. Solo hay una obra simultánea por ciudad. La mano de obra conserva su representación actual como recurso acumulable; no se ha convertido en trabajadores ocupados o población disponible.

## Paneles

- **Ciudad:** viviendas, edificios terminados, obra en curso, aportación y mantenimiento de esa ciudad.
- **Edificios:** coste, duración, beneficio, mantenimiento, déficit exacto de recursos y detalles del edificio seleccionado. Una obra en curso indica progreso y turno de finalización; su coste ya está abonado.
- **Hacienda:** reservas del reino, previsión de reservas al siguiente turno y su variación neta, desglose de rentas territoriales y edificios propios, mantenimiento y déficit si existe.
- **Aviso inferior:** obra en curso o finalizada. El aviso de finalización se conserva hasta el siguiente avance de turno y se puede ver al reabrir la ciudad; no se guarda como evento histórico.
- **Mapa:** el resumen del próximo cierre descuenta mantenimiento en su previsión de grano y oro. El desglose completo está en Hacienda.

Se mantiene la inspección de ciudades ajenas sin permitir construir, gastar o avanzar turnos desde ellas. El crecimiento visible, los andamios y las limitaciones de cámara siguen usando los recursos existentes.

## Guardado

El controlador escribe el formato v5 con propiedad de asentamientos. Ingresos y mantenimiento se derivan de los edificios terminados, los distritos y sus propietarios: no se guardan bonificaciones acumuladas que puedan aplicarse dos veces. Al cargar se validan propietarios, edificios y obras antes de sustituir la campaña, y se reconstruye su economía.

Los guardados v1 y v2 siguen siendo compatibles en modo simple y restauran la ciudad inicial, sin conservar efectos de edificios de la partida reemplazada. Una nueva partida restablece las rentas base y elimina obras y efectos anteriores. El [guardado multihueste v4](GUARDADO_MULTIHUESTE.md) también conserva ciudades y obras, y reconstruye su economía sin duplicarla.

## Validación

345 pruebas EditMode superadas, sin fallos ni omitidas. Incluyen efectos al completar obras, mantenimiento, previsión frente a reservas reales, insuficiencia de recursos, carga repetida sin duplicaciones, obra pendiente al cargar, compatibilidad, nueva partida, ciudades ajenas, cierre multihueste, navegación de Hacienda y transferencia de rentas y edificios al conquistar. Los resultados y ejecutables de revisión se guardan fuera del repositorio.
