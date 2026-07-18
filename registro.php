<?php
require __DIR__ . '/conexion.php';
$reinos = require __DIR__ . '/datos_reinos.php';
$pdo = conectar();

// De momento hay una unica partida local: si ya existe un reino no se
// puede crear otro (el login multiusuario llega en su propio hito)
$partida = $pdo->query('SELECT id FROM reinos ORDER BY id LIMIT 1')->fetch();

$error = null;

if ($_SERVER['REQUEST_METHOD'] === 'POST' && !$partida) {
    $jugador = trim($_POST['jugador'] ?? '');
    $reino = $_POST['reino'] ?? '';

    if ($jugador === '') {
        $error = 'Escribe el nombre de tu dinastía.';
    } elseif (!isset($reinos[$reino])) {
        $error = 'Elige uno de los cinco reinos.';
    } else {
        // Jugador y reino se crean juntos o no se crean: transacción
        $pdo->beginTransaction();
        $pdo->prepare('INSERT INTO jugadores (nombre) VALUES (?)')
            ->execute([$jugador]);
        $jugadorId = (int) $pdo->lastInsertId();
        $pdo->prepare(
            'INSERT INTO reinos (jugador_id, reino, color, nombre) VALUES (?, ?, ?, ?)'
        )->execute([$jugadorId, $reino, $reinos[$reino]['color'], $reinos[$reino]['nombre']]);
        $pdo->commit();

        header('Location: reino.php');
        exit;
    }
}
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Fulgor Medieval — Construye tu reino</title>
    <link rel="stylesheet" href="static/css/estilo.css">
</head>
<body>
    <main class="registro">
        <h1>Construye tu reino</h1>
        <p class="subtitulo">Los sabios dicen que el reino que reúna las tres llamas
        — prosperar, construir y proteger — verá renacer el Fulgor Dorado.</p>

        <?php if ($partida): ?>
            <p class="aviso">Ya hay una partida en curso. Cárgala desde el menú.</p>
            <div class="acciones">
                <a class="boton secundario" href="menu.php">Atrás</a>
            </div>
        <?php else: ?>
            <?php if ($error): ?>
                <p class="error"><?= htmlspecialchars($error) ?></p>
            <?php endif; ?>

            <form method="post" action="registro.php">
                <div class="formulario-jugador">
                    <label for="jugador">Tu dinastía:</label>
                    <input type="text" id="jugador" name="jugador" maxlength="50"
                           placeholder="Nombre de tu dinastía" required>
                </div>

                <h2>Elige tu reino</h2>
                <div class="fichas">
                    <?php foreach ($reinos as $slug => $r): ?>
                        <div class="ficha-envoltura">
                            <input type="radio" name="reino" id="reino-<?= $slug ?>"
                                   value="<?= $slug ?>" required>
                            <label class="ficha" for="reino-<?= $slug ?>"
                                   style="--color-reino: <?= $r['color_hex'] ?>">
                                <h3><?= $r['nombre'] ?></h3>
                                <p class="lema"><?= $r['lema'] ?></p>
                                <p class="bandera">⚑ <?= $r['bandera'] ?></p>
                                <p class="lore"><?= $r['lore'] ?></p>
                                <p class="heroe"><strong>Héroe:</strong> <?= $r['heroe'] ?></p>
                                <p class="tropa"><strong>Élite:</strong> <?= $r['tropa'] ?></p>
                                <p class="color-linea"><strong>Color:</strong> <?= $r['color'] ?></p>
                            </label>
                        </div>
                    <?php endforeach; ?>
                </div>

                <div class="acciones">
                    <a class="boton secundario" href="menu.php">Atrás</a>
                    <button class="boton" type="submit">Empezar partida</button>
                </div>
            </form>
        <?php endif; ?>
    </main>
</body>
</html>
