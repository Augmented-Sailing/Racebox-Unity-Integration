using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    ///     Performs additional visual operations on the ARFeatheredPlane mesh, such as animated alpha fading.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ARFeatheredPlaneMeshVisualizerCompanion : MonoBehaviour
    {
        [Tooltip("Renderer component on the ARFeatheredPlane prefab. Used to fetch the material to fade in/out.")]
        [SerializeField]
        private Renderer m_PlaneRenderer;

        [Tooltip(
            "Fade in/out speed multiplier applied during the alpha tweening. The lower the value, the slower it works. A value of 1 is full speed (1 second).")]
        [Range(0.1f, 1.0f)]
        [SerializeField]
        private float m_FadeSpeed = 1f;

#pragma warning disable CS0618 // Type or member is obsolete -- affordance system to be replaced in a future XRI version
        private readonly FloatTweenableVariable m_AlphaTweenableVariable = new();
#pragma warning restore CS0618
        private Material m_PlaneMaterial;

        private int m_ShaderAlphaPropertyID;
        private float m_SurfaceVisualAlpha = 1f;
        private float m_TweenProgress;

        /// <summary>
        ///     The Renderer component on the ARFeatheredPlane prefab. Used to fetch the material to fade in/out.
        /// </summary>
        public Renderer planeRenderer
        {
            get => m_PlaneRenderer;
            set => m_PlaneRenderer = value;
        }

        /// <summary>
        ///     Fade in/out speed multiplier applied during the alpha tweening.
        ///     The lower the value, the slower it works. A value of 1 is full speed (1 second).
        /// </summary>
        public float fadeSpeed
        {
            get => m_FadeSpeed;
            set => m_FadeSpeed = value;
        }

        /// <summary>
        ///     Show plane surfaces if true, hide plane surfaces if false
        /// </summary>
        public bool visualizeSurfaces
        {
            set
            {
                m_TweenProgress = 0f;
                m_AlphaTweenableVariable.target = value ? 1f : 0f;
                m_AlphaTweenableVariable.HandleTween(0f);
            }
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void Awake()
        {
            m_ShaderAlphaPropertyID = Shader.PropertyToID("_PlaneAlpha");
            m_PlaneMaterial = m_PlaneRenderer.material;
            visualizeSurfaces = true;
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void Update()
        {
            m_AlphaTweenableVariable.HandleTween(m_TweenProgress);
            m_TweenProgress += Time.unscaledDeltaTime * m_FadeSpeed;
            m_SurfaceVisualAlpha = m_AlphaTweenableVariable.Value;
            m_PlaneMaterial.SetFloat(m_ShaderAlphaPropertyID, m_SurfaceVisualAlpha);
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void OnDestroy()
        {
            m_AlphaTweenableVariable.Dispose();
        }
    }
}