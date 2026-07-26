# Seguridad

## Versiones mantenidas

Mientras el proyecto esté en pre-alpha, las correcciones se aplican sobre
`main`.

## Reporte responsable

No publiques una vulnerabilidad con datos sensibles en una incidencia pública.
Utiliza el canal privado de reporte de seguridad del repositorio de GitHub.

## Secretos

- Nunca se guardan tokens, contraseñas ni claves privadas en el repositorio.
- Los valores locales viven en `.env`, que está ignorado por Git.
- `.env.example` solo puede contener nombres y ejemplos no sensibles.
- Una credencial expuesta debe revocarse; borrarla del último commit no basta.
- Antes de publicar se revisan el árbol actual y el historial.

## Datos del jugador

El prototipo no solicita cuentas ni transmite datos personales. Los guardados
son locales. Si se añade un backend deberán documentarse autenticación,
autorización, validación de entrada, retención y borrado de datos.

## Dependencias y contenido

- Las actualizaciones se validan mediante lint y build.
- No se ejecutan archivos o workflows obtenidos de fuentes desconocidas.
- Los recursos artísticos deben tener procedencia y permisos documentados.
- Los archivos audiovisuales de trabajo no se incluyen en Git.
