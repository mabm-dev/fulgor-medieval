# Fulgor Medieval — prototipo Unity

Proyecto de migración a Unity 6000.6.0f1 con URP. El mapa de campaña actual
es un prototipo técnico de 24×16 hexágonos con generación determinista,
relieve provisional, costas, selección y cámara. Aún no hay bucle jugable de
campaña ni arte de producción.

La documentación vigente está en
[Docs/migracion-unity](Docs/migracion-unity/README.md). El
[contrato Blender–Unity](Docs/CONTRATO_VISUAL_BLENDER_UNITY.md) fija escala,
ejes, pivotes y límites iniciales para los recursos.

Para inspeccionar el prototipo, abrir el proyecto con Unity 6000.6.0f1,
cargar Assets/Fulgor/Scenes/CampaignPrototype.unity y entrar en Play.
Las pruebas Edit Mode cubren Core, Presentation, importación y escena.
La última validación batch aislada superó 94/94; sigue pendiente una
revisión visual en Game View y mediciones de rendimiento en build.
