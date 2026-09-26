using System;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    /// <summary>Estado de selección independiente de la entrada y de la escena.</summary>
    public sealed class MapSelection
    {
        public event Action<MapCell?> Changed;

        public MapCell? SelectedCell { get; private set; }

        public void Select(MapCell cell)
        {
            if (SelectedCell.HasValue && SelectedCell.Value.Equals(cell)) return;
            SelectedCell = cell;
            Changed?.Invoke(SelectedCell);
        }

        public void Clear()
        {
            if (!SelectedCell.HasValue) return;
            SelectedCell = null;
            Changed?.Invoke(null);
        }
    }
}
