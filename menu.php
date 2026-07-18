<?php
require __DIR__ . '/conexion.php';
$pdo = conectar();

// ¿Hay una partida creada? De ella depende que "Cargar partida" este activa
$partida = $pdo->query('SELECT id FROM reinos ORDER BY id LIMIT 1')->fetch();
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Fulgor Medieval — Menú</title>
    <link rel="stylesheet" href="static/css/estilo.css">
</head>
<body>
    <div class="fondo-dinamico"></div>
    <main class="centrado">
        <div class="panel">
            <h1>Fulgor Medieval</h1>
            <p class="turno">El Fulgor no se hereda — se construye.</p>
            <div class="botones">
                <a class="boton-grande" href="registro.php">Construye tu reino</a>
                <?php if ($partida): ?>
                    <a class="boton-grande" href="reino.php">Cargar partida</a>
                <?php else: ?>
                    <span class="boton-grande bloqueado">🔒 Cargar partida</span>
                <?php endif; ?>
            </div>
        </div>
    </main>
</body>
</html>
