# Seguridad

## Versiones mantenidas

Mientras el proyecto esté en pre-alpha, las correcciones se aplican sobre
`main`. El prototipo web de la rama `legacy/web` no recibe correcciones.

## Reporte responsable

No publiques una vulnerabilidad con datos sensibles en una incidencia pública.
Utiliza el canal privado de reporte de seguridad del repositorio de GitHub.

## Secretos

- Nunca se guardan tokens, contraseñas, claves privadas ni archivos de licencia
  de Unity en el repositorio.
- La integración continua recibe la licencia y las credenciales de Unity como
  secretos cifrados de GitHub Actions.
- Una credencial expuesta debe revocarse; borrarla del último commit no basta.
- Antes de publicar se revisan el árbol actual y el historial.

## Datos del jugador

El prototipo no solicita cuentas ni transmite datos personales. Los guardados
son locales. Si se añade un backend deberán documentarse autenticación,
autorización, validación de entrada, retención y borrado de datos.

## Dependencias y contenido

- Las actualizaciones de Unity y de sus paquetes se validan con las pruebas y
  una build antes de fusionarse.
- Las acciones de terceros de los workflows se fijan por versión.
- No se ejecutan archivos, paquetes ni workflows obtenidos de fuentes
  desconocidas.
- Los recursos artísticos deben tener procedencia y permisos documentados.
- Los archivos audiovisuales de trabajo no se incluyen en Git.
