using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public enum FulgorIcon { Grain, Wood, Stone, Gold, Labor, Population, Turn }

    /// <summary>
    /// Iconos y marcos dorados del HUD. Salen de ArtSource/UI/generar_interfaz_fulgor.py
    /// y viven en Resources/LegacyUI/Icons; si faltan, el HUD sigue sin ellos.
    /// </summary>
    public static class FulgorIcons
    {
        public const string Folder = "LegacyUI/Icons";
        private static readonly Dictionary<string, Sprite> cache = new();

        public static readonly FulgorIcon[] Resources5 =
            { FulgorIcon.Grain, FulgorIcon.Wood, FulgorIcon.Stone, FulgorIcon.Gold, FulgorIcon.Labor };

        public static string File(FulgorIcon icon) => icon switch
        {
            FulgorIcon.Grain => "icono_grano",
            FulgorIcon.Wood => "icono_madera",
            FulgorIcon.Stone => "icono_piedra",
            FulgorIcon.Gold => "icono_oro",
            FulgorIcon.Labor => "icono_mano_de_obra",
            FulgorIcon.Population => "icono_poblacion",
            FulgorIcon.Turn => "icono_turno",
            _ => throw new System.ArgumentOutOfRangeException(nameof(icon)),
        };

        public static Sprite Get(FulgorIcon icon) => Load(File(icon), 0f);
        public static Sprite PanelFrame => Load("panel", 24f);
        public static Sprite ChipFrame => Load("ficha", 12f);

        /// <summary>Imagen de icono anclada arriba a la izquierda de su padre.</summary>
        public static Image Create(string name, Transform parent, FulgorIcon icon, Vector2 position, float size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;
            var image = item.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = Get(icon);
            image.enabled = image.sprite != null;
            return image;
        }

        private static Sprite Load(string file, float border)
        {
            if (cache.TryGetValue(file, out var cached) && cached != null) return cached;
            var sprite = UnityEngine.Resources.Load<Sprite>($"{Folder}/{file}");
            if (sprite == null)
            {
                // Respaldo si el importador no la marcó como sprite.
                var texture = UnityEngine.Resources.Load<Texture2D>($"{Folder}/{file}");
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                        new Vector4(border, border, border, border));
            }
            cache[file] = sprite;
            return sprite;
        }
    }
}
