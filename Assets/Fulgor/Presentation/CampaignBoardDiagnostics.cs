using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public readonly struct BoardBuildMetrics
    {
        public BoardBuildMetrics(double milliseconds, long managedBytes, int tiles,
            int transitionInstances, int junctionInstances, int goldMarkers,
            int uniqueMeshes, int sharedMaterials)
        {
            Milliseconds = milliseconds;
            ManagedBytes = managedBytes;
            Tiles = tiles;
            TransitionInstances = transitionInstances;
            JunctionInstances = junctionInstances;
            GoldMarkers = goldMarkers;
            UniqueMeshes = uniqueMeshes;
            SharedMaterials = sharedMaterials;
        }

        public double Milliseconds { get; }
        public long ManagedBytes { get; }
        public int Tiles { get; }
        public int TransitionInstances { get; }
        public int JunctionInstances { get; }
        public int GoldMarkers { get; }
        public int UniqueMeshes { get; }
        public int SharedMaterials { get; }
        public int Renderers => Tiles + TransitionInstances + JunctionInstances + GoldMarkers;
        public int Colliders => Tiles;
    }

    public sealed class CampaignBoardDiagnostics : MonoBehaviour
    {
        public bool Completed { get; private set; }
        public BoardBuildMetrics Metrics { get; private set; }

        public void Record(BoardBuildMetrics metrics)
        {
            Metrics = metrics;
            Completed = true;
            Debug.Log($"Fulgor board: {metrics.Tiles} tiles, {metrics.Renderers} renderers, " +
                $"{metrics.Colliders} colliders, {metrics.UniqueMeshes} shared meshes, " +
                $"{metrics.SharedMaterials} shared materials, {metrics.Milliseconds:F1} ms, " +
                $"managed delta {metrics.ManagedBytes / 1024f:F1} KiB.", this);
        }
    }
}
