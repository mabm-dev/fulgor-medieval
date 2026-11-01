"""Plugin de Substance 3D Painter: añade al menú Edit las acciones de Fulgor para la fortaleza de Castilla.

Painter 12 no tiene consola de Python interactiva; los scripts se ejecutan como plugins. Este archivo se copia a
  %USERPROFILE%\\Documents\\Adobe\\Adobe Substance 3D Painter\\python\\plugins
y se activa desde el menú Python de Painter (Reload Plugins Folder y marcar "fulgor_painter_plugin").
Después aparecen en Edit las acciones "Fulgor: preparar fortaleza de Castilla" y "Fulgor: exportar a Unity".
La salida de los scripts va a la ventana de registro (Window > Views > Log).
"""
import runpy
import traceback
from pathlib import Path

from PySide6 import QtGui

import substance_painter.ui as ui

ROOT = Path(r'D:\Proyectos\fulgor-medieval-unity\.worktrees\facciones')
ACCIONES = [
    ('Fulgor: preparar fortaleza de Castilla', ROOT / 'Tools/Painter/preparar_castilla.py'),
    ('Fulgor: exportar a Unity', ROOT / 'Tools/Painter/exportar_castilla_unity.py'),
]
_acciones = []


def _ejecutar(ruta):
    try:
        print('Ejecutando', ruta)
        runpy.run_path(str(ruta), run_name='__main__')
    except Exception:
        print('ERROR en', ruta)
        print(traceback.format_exc())


def start_plugin():
    for etiqueta, ruta in ACCIONES:
        accion = QtGui.QAction(etiqueta)
        accion.triggered.connect(lambda _=False, r=ruta: _ejecutar(r))
        ui.add_action(ui.ApplicationMenu.Edit, accion)
        _acciones.append(accion)


def close_plugin():
    for accion in _acciones:
        ui.delete_ui_element(accion)
    _acciones.clear()


if __name__ == '__main__':
    start_plugin()
