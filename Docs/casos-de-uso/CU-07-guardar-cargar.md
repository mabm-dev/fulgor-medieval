# CU-07: guardar y cargar

## Objetivo

Continuar una campaña sin pérdida ni corrupción silenciosa.

## Alcance implementado en v0.3

La aplicación mantiene un único guardado automático de la sesión actual. La
instantánea incluye la versión del esquema, el turno, la fase, el reino jugador
y sus cinco recursos.

Al entrar en el mapa, el guardado se valida antes de restaurarse. Una nueva
partida elimina el estado anterior para evitar mezclar dos campañas.

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
