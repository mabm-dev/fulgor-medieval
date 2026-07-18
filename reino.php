<?php
require __DIR__ . '/conexion.php';
$reinos = require __DIR__ . '/datos_reinos.php';
$pdo = conectar();

$reino = $pdo->query(
    'SELECT r.*, j.nombre AS jugador
     FROM reinos r
     JOIN jugadores j ON j.id = r.jugador_id
     ORDER BY r.id LIMIT 1'
)->fetch();

if ($reino === false) {
    header('Location: menu.php');
    exit;
}

$datos = $reinos[$reino['reino']];
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Fulgor Medieval — <?= htmlspecialchars($reino['nombre']) ?></title>
    <link rel="stylesheet" href="static/css/estilo.css">
</head>
<body>
    <main class="centrado">
        <div class="panel" style="--color-reino: <?= $datos['color_hex'] ?>">
            <h1 class="nombre-reino"><?= htmlspecialchars($reino['nombre']) ?></h1>
            <p class="lema-grande"><?= $datos['lema'] ?></p>
            <p class="detalle-reino">Dinastía <?= htmlspecialchars($reino['jugador']) ?>
                · Héroe: <?= $datos['heroe'] ?></p>
            <p class="turno">Turno <?= $reino['turno'] ?></p>

            <section class="recursos">
                <div class="recurso">
                    <span class="icono">🌲</span>
                    <span class="cantidad"><?= $reino['madera'] ?></span>
                    <span class="nombre">Madera</span>
                </div>
                <div class="recurso">
                    <span class="icono">🪨</span>
                    <span class="cantidad"><?= $reino['piedra'] ?></span>
                    <span class="nombre">Piedra</span>
                </div>
                <div class="recurso">
                    <span class="icono">🌾</span>
                    <span class="cantidad"><?= $reino['comida'] ?></span>
                    <span class="nombre">Comida</span>
                </div>
                <div class="recurso">
                    <span class="icono">💧</span>
                    <span class="cantidad"><?= $reino['agua'] ?></span>
                    <span class="nombre">Agua</span>
                </div>
            </section>

            <!-- Aqui ira el boton "Pasar turno" (siguiente paso, CU-01) -->
            <p class="volver"><a href="menu.php">← Volver al menú</a></p>
        </div>
    </main>
</body>
</html>
