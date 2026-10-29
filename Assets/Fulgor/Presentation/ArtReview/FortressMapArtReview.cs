using System.Collections;
using System.Linq;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Places one art sample in the dedicated review scene after the campaign board is ready.</summary>
    public sealed class FortressMapArtReview : MonoBehaviour
    {
        [SerializeField] private GameObject fortressPrefab;
        public bool PlacementReady { get; private set; }
        public Transform PlacedFortress { get; private set; }
        public HexTileView ReviewTile { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            var board = FindAnyObjectByType<CampaignBoardPreview>();
            if (board == null || fortressPrefab == null)
            {
                Debug.LogError("La escena de revision necesita tablero y prefab de fortaleza.", this);
                yield break;
            }
            ReviewTile = board.GetComponentsInChildren<HexTileView>()
                .Where(tile => tile.Cell.Terrain == TerrainType.Plain && !tile.Cell.HasGold)
                .OrderBy(tile => Mathf.Pow(tile.Cell.Coordinates.Q - 12, 2) +
                                 Mathf.Pow(tile.Cell.Coordinates.R - 8, 2))
                .FirstOrDefault();
            if (ReviewTile == null)
            {
                Debug.LogError("No hay una llanura disponible para revisar la fortaleza.", this);
                yield break;
            }
            PlacedFortress = Instantiate(fortressPrefab, transform).transform;
            PlacedFortress.localScale = Vector3.one;
            PlacedFortress.position = ReviewTile.transform.position +
                Vector3.up * (TerrainVisualStyle.For(ReviewTile.Cell.Terrain).Height + .001f);
            PlacedFortress.rotation = Quaternion.identity;
            var camera = Camera.main;
            if (camera != null)
            {
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.FocusOnWorld(PlacedFortress.position);
            }
            PlacementReady = true;
        }
    }
}
