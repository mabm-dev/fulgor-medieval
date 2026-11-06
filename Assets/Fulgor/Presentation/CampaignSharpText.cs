using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Texto de interfaz con más peso y contraste para fondos ilustrados.</summary>
    public sealed class CampaignSharpText : Text
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            fontStyle = FontStyle.Bold;
            alignByGeometry = true;

            var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.025f, 0.018f, 0.012f, 0.88f);
            outline.effectDistance = new Vector2(0.8f, -0.8f);
            outline.useGraphicAlpha = true;
        }
    }
}
