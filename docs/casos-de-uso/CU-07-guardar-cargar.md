# CU-07: guardar y cargar

## Objetivo

Continuar una campaña sin pérdida ni corrupción silenciosa.

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
