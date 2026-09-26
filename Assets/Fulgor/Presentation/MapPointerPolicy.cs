using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public static class MapPointerPolicy
    {
        public static Vector2 EdgeDirection(Vector2 pointer, Rect viewport, float edgeSize)
        {
            if (edgeSize <= 0f || !viewport.Contains(pointer)) return Vector2.zero;
            float edge = Mathf.Min(edgeSize, Mathf.Min(viewport.width, viewport.height) * 0.5f);
            float x = pointer.x < viewport.xMin + edge ? -1f : pointer.x >= viewport.xMax - edge ? 1f : 0f;
            float y = pointer.y < viewport.yMin + edge ? -1f : pointer.y >= viewport.yMax - edge ? 1f : 0f;
            return new Vector2(x, y);
        }

        public static bool IsOverPanel(Vector2 screenPosition, float screenHeight, Rect guiPanel)
        {
            return guiPanel.Contains(new Vector2(screenPosition.x, screenHeight - screenPosition.y));
        }
    }
}