<?php
require __DIR__ . '/conexion.php';

$pdo = conectar();
$reino = $pdo->query('SELECT * FROM reinos WHERE id = 1')->fetch();

if ($reino === false) {
    die('No hay ningun reino creado. Ejecuta el SQL de docs/sql/.');
}
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
    <main class="panel">
        <h1><?= htmlspecialchars($reino['nombre']) ?></h1>
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
    </main>
</body>
</html>
