using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Texto de interfaz con más peso y contraste para fondos ilustrados. Una sola sombra a un píxel entero en lugar del contorno de 0,8 px:
    /// el contorno dibujaba cuatro copias del texto desplazadas medio píxel y emborronaba las letras pequeñas. Ningún texto baja de
    /// <see cref="MinimumSize"/> puntos.
    /// </summary>
    public sealed class CampaignSharpText : Text
    {
        public const int MinimumSize = 14;

        protected override void OnEnable()
        {
            base.OnEnable();
            fontStyle = FontStyle.Bold;
            alignByGeometry = false; // alinear a la rejilla de píxeles, no a la silueta de las letras

            var outline = GetComponent<Outline>();
            if (outline != null) { outline.enabled = false; if (Application.isPlaying) Destroy(outline); else DestroyImmediate(outline); }
            var shadow = GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.015f, 0.01f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
        }

        // Quien crea el texto fija su tamaño justo después de añadirlo; el mínimo se aplica en el primer fotograma.
        protected override void Start()
        {
            base.Start();
            if (fontSize > 1 && fontSize < MinimumSize) fontSize = MinimumSize;
        }
    }
}
