# CU-07: guardar y cargar

## Objetivo

Continuar una campaña sin pérdida ni corrupción silenciosa.

## Alcance por versión

Desde v0.3, el prototipo web mantenía un único guardado automático de la
sesión, validado antes de restaurarse, y una nueva partida eliminaba el estado
anterior para no mezclar dos campañas. En Unity, el guardado local versionado
llega en v0.8, según el [roadmap](../ROADMAP.md); su formato está por diseñar y
no importará partidas del prototipo web.

La selección entre varias campañas, las migraciones entre versiones y la
confirmación visual de borrado pertenecen a versiones posteriores.

## Flujo principal

1. El juego crea una instantánea tras un punto seguro.
2. La instantánea incluye versión, semilla y estado completo.
3. El menú muestra campaña, facción, turno y fecha.
4. El jugador selecciona continuar.
5. El juego valida y, si procede, migra el guardado.
6. La campaña se restaura.

## Criterios de aceptación

- Un guardado incompatible explica el problema.
- Una escritura fallida no destruye la copia anterior.
- El guardado no contiene credenciales ni datos personales innecesarios.
- Borrar exige confirmación.
