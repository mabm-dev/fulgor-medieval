using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fulgor.Presentation.Map
{
    /// <summary>Reproduce la marcha del grupo de soldados sin AnimatorController, avanzada a mano desde la hueste.</summary>
    /// <remarks>Fotograma 0 del clip: pose neutra (parados). Del 1 al final: ciclo en bucle de dos pasos.</remarks>
    [RequireComponent(typeof(Animator))]
    public sealed class CampaignSoldierWalk : MonoBehaviour
    {
        [SerializeField] private AnimationClip clip;
        // La hueste cruza el mapa más deprisa de lo que un paso real avanzaría; se acelera el ciclo para que no patine tanto.
        [SerializeField, Min(.1f)] private float playbackSpeed = 3f;

        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private float time;

        public AnimationClip Clip { get => clip; set => clip = value; }
        public bool Walking { get; private set; }

        public void Advance(bool walking, float seconds)
        {
            Walking = walking;
            if (!Ensure()) return;
            if (walking)
            {
                float start = 1f / clip.frameRate;
                time = Mathf.Max(start, time + Mathf.Max(0f, seconds) * playbackSpeed);
                while (time >= clip.length) time -= clip.length - start;
            }
            else time = 0f;
            playable.SetTime(time);
            graph.Evaluate(0f);
        }

        private bool Ensure()
        {
            if (graph.IsValid()) return true;
            var animator = GetComponent<Animator>();
            if (clip == null || animator == null) return false;
            animator.runtimeAnimatorController = null;
            graph = PlayableGraph.Create("Marcha de la hueste");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "Marcha", animator).SetSourcePlayable(playable);
            return true;
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
