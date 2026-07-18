-- Fulgor Medieval - Hito 1: creacion de la base de datos
--
-- Como ejecutarlo (en WSL/Ubuntu):
--   sudo mariadb < docs/sql/01-crear-base-de-datos.sql
--
-- OJO: antes de ejecutarlo, cambia PON_AQUI_UNA_CONTRASENA_SEGURA
-- por una contrasena real. La contrasena verdadera NUNCA se sube
-- a GitHub: vive en el archivo .env (ver .env.example).

CREATE DATABASE IF NOT EXISTS fulgor_medieval
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Usuario del juego: permisos solo sobre esta base de datos
CREATE USER IF NOT EXISTS 'fulgor_user'@'localhost'
  IDENTIFIED BY 'PON_AQUI_UNA_CONTRASENA_SEGURA';
GRANT ALL PRIVILEGES ON fulgor_medieval.* TO 'fulgor_user'@'localhost';
FLUSH PRIVILEGES;

-- Hito 1: el reino y sus recursos (caso de uso CU-01)
CREATE TABLE fulgor_medieval.reinos (
  id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  nombre VARCHAR(50) NOT NULL,
  madera INT NOT NULL DEFAULT 100,
  piedra INT NOT NULL DEFAULT 100,
  comida INT NOT NULL DEFAULT 100,
  agua INT NOT NULL DEFAULT 100,
  turno INT NOT NULL DEFAULT 0,
  creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  actualizado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    ON UPDATE CURRENT_TIMESTAMP
);

-- Reino de prueba para desarrollo
INSERT INTO fulgor_medieval.reinos (nombre) VALUES ('Castilla');
