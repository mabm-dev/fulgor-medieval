# Auditoría de vigencia de retratos

Fecha de revisión: 22 de octubre de 2026.

## Hechos comprobados

- Repositorio activo: el repositorio Unity.
- Rama de trabajo: perf/bloque-color-casillas-v0.7.
- HEAD local: a6e1255ab7389eb38c60edcd15c55020df75997b.
- Se actualizan referencias remotas con git fetch origin.
- origin/main: ac42f0f, revisión de Unity del 21 de octubre.
- origin/perf/bloque-color-casillas-v0.7: 601d550.
- origin/legacy/web: 884ec5517e246cfaa918131f0c768cd08000d34b.
- el repositorio web legacy conserva una copia de la web archivada.
- Los cinco retratos y sus siluetas consultados están en public/imagenes de
  legacy/web. No son prueba de diseños actuales de la versión Unity.
- En los árboles actuales examinados de Unity no aparecen otros retratos.
- Docs/diseno/facciones-y-lideres.md describe doce líderes, con nombres distintos
  del reparto de cinco héroes de la web.
- No se ha generado una lámina de Castilla ni alterado un personaje tras
  la advertencia del usuario.

## Resultado

Vigencia de las imágenes legacy **no confirmada**. No convertirlas en referencia
canónica por el hecho de estar conservadas en Git. Se mantienen sus copias con
hash verificado como material histórico. Pendiente de ubicación/confirmación
de las imágenes actuales antes de continuar el piloto de identidad.

## Resolución tras recibir la actualización de main

El commit `6f302d6`, descargado después de la búsqueda anterior, incorpora
los doce PNG vigentes en `ArtSource/Portraits/Heroes/` y declara expresamente
que sustituyen a los de la web. No proceden de una distribución WSL local.

Se integra origin/perf con `af2dbf9` y origin/main con `15f7a1f`.
Antes se guarda parche local, ASSETS y hashes en .utmp/sync-portraits-*.
Se comprueba que los nueve archivos modificados distintos de ASSETS mantienen
su SHA-256 y que siguen presentes los 745 archivos antes sin seguimiento.
ASSETS recupera el contenido local e incorpora la fila y nota de los retratos.

Los doce originales coinciden con sus blobs de main. La lámina de Rodrigo se
genera usando el PNG vigente, no el retrato web ni el capitán genérico.
