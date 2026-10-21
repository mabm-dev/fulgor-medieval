#!/usr/bin/env bash
# Ejecuta las pruebas Edit Mode de Fulgor en una máquina Linux con Unity instalado
# (el CT fulgor-ci del servidor). Conserva Library/ entre ejecuciones y deja el
# clon limpio al terminar.
#
# Uso: Tools/ci/run-editmode-tests.sh [rama|commit]   (por defecto: origin/main)
# Salida: 0 si todas las pruebas pasan; el código de Unity en otro caso
#         (2 = alguna prueba falló, 1 = error del editor o de la licencia).
set -euo pipefail

# El script cambia el clon de commit y bash lo lee mientras se ejecuta:
# se relanza desde una copia temporal para que el checkout no lo modifique.
if [ -z "${FULGOR_TESTS_COPY:-}" ]; then
  COPY="$(mktemp)"
  cp "$0" "$COPY"
  FULGOR_TESTS_COPY="$COPY" exec bash "$COPY" "$@"
fi
trap 'rm -f "$FULGOR_TESTS_COPY"' EXIT

UNITY_EDITOR="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity}"
PROJECT_DIR="${PROJECT_DIR:-$HOME/fulgor-medieval}"
RESULTS_DIR="${RESULTS_DIR:-$HOME/test-results}"
REF="${1:-origin/main}"

fail() { echo "ERROR: $*" >&2; exit 1; }

[ -x "$UNITY_EDITOR" ] || fail "no se encuentra Unity en $UNITY_EDITOR (define UNITY_EDITOR)"
git -C "$PROJECT_DIR" rev-parse --git-dir >/dev/null 2>&1 || fail "$PROJECT_DIR no es un clon de Git"

exec 9>"/tmp/fulgor-editmode-tests.lock"
flock -n 9 || fail "ya hay otra ejecución de las pruebas en marcha"

if [ -n "$(git -C "$PROJECT_DIR" status --porcelain --untracked-files=no)" ]; then
  git -C "$PROJECT_DIR" status --short --untracked-files=no >&2
  fail "el clon tiene cambios sin confirmar; revísalos antes de lanzar las pruebas"
fi

git -C "$PROJECT_DIR" fetch --prune --quiet origin
if git -C "$PROJECT_DIR" rev-parse --verify --quiet "origin/$REF^{commit}" >/dev/null; then
  REF="origin/$REF"
fi
COMMIT="$(git -C "$PROJECT_DIR" rev-parse --verify --quiet "$REF^{commit}")" || fail "no existe la referencia $REF"
git -C "$PROJECT_DIR" checkout --quiet --detach "$COMMIT"

# Unity reescribe algunos archivos versionados al abrir el proyecto
# (por ejemplo m_RuntimeSettings de URP); se deshacen siempre al salir.
restore_tree() { git -C "$PROJECT_DIR" restore --worktree --staged -- . 2>/dev/null || true; }
trap 'restore_tree; rm -f "$FULGOR_TESTS_COPY"' EXIT

mkdir -p "$RESULTS_DIR"
STAMP="$(date +%Y%m%d-%H%M%S)-$(git -C "$PROJECT_DIR" rev-parse --short "$COMMIT")"
RESULTS="$RESULTS_DIR/editmode-$STAMP.xml"
LOG="$RESULTS_DIR/editmode-$STAMP.log"

echo "Pruebas Edit Mode de $REF ($(git -C "$PROJECT_DIR" log -1 --format='%h %s' "$COMMIT"))"
set +e
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PROJECT_DIR" \
  -runTests -testPlatform EditMode -testResults "$RESULTS" -logFile "$LOG"
CODE=$?
set -e

if [ -f "$RESULTS" ]; then
  SUMMARY="$(grep -m 1 '<test-run' "$RESULTS")"
  field() { sed -n "s/.* $1=\"\([^\"]*\)\".*/\1/p" <<<"$SUMMARY"; }
  echo "Resultado: $(field result) · total $(field total) · pasadas $(field passed) · fallidas $(field failed) · omitidas $(field skipped)"
  echo "Informe: $RESULTS"
else
  echo "Unity no generó informe de pruebas (código $CODE). Revisa el log." >&2
fi
echo "Log: $LOG"
exit "$CODE"
