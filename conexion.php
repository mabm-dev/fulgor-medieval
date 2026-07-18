<?php
// Conexion a la base de datos del juego.
// Lee las credenciales del archivo .env (que nunca se sube a GitHub).

function conectar(): PDO
{
    $config = parse_ini_file(__DIR__ . '/.env');

    if ($config === false) {
        throw new RuntimeException(
            'No se encuentra el archivo .env. '
            . 'Copia .env.example como .env y rellena los valores.'
        );
    }

    $dsn = sprintf(
        'mysql:host=%s;dbname=%s;charset=utf8mb4',
        $config['DB_HOST'],
        $config['DB_NAME']
    );

    return new PDO($dsn, $config['DB_USER'], $config['DB_PASS'], [
        // Los errores de base de datos lanzan excepciones (no fallan en silencio)
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        // fetch() devuelve arrays asociativos: $reino['madera']
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);
}
