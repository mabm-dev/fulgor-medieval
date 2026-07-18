-- Fulgor Medieval - Secuencia de inicio: jugadores y reinos ligados
--
-- Como ejecutarlo (en WSL/Ubuntu):
--   mariadb -u fulgor_user -p fulgor_medieval < docs/sql/02-jugadores.sql
--
-- Crea la tabla de jugadores y liga cada reino a su jugador.
-- El registro del jugador ES el login de momento; el login con
-- contrasena llega en el hito de multiusuario.

USE fulgor_medieval;

CREATE TABLE jugadores (
  id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  nombre VARCHAR(50) NOT NULL,
  creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE reinos
  ADD COLUMN jugador_id INT UNSIGNED NULL AFTER id,
  ADD COLUMN reino VARCHAR(20) NOT NULL DEFAULT 'castilla' AFTER jugador_id,
  ADD COLUMN color VARCHAR(20) NOT NULL DEFAULT 'carmesi' AFTER reino,
  ADD CONSTRAINT fk_reinos_jugador
    FOREIGN KEY (jugador_id) REFERENCES jugadores (id);

-- Se borra la Castilla de prueba del script 01:
-- a partir de aqui los reinos nacen del registro en el juego
DELETE FROM reinos;
ALTER TABLE reinos AUTO_INCREMENT = 1;
