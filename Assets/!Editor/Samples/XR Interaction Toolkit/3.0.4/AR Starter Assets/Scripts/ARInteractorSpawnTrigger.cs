#if AR_FOUNDATION_PRESENT
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    ///     Spawns an object at an <see cref="IARInteractor" />'s raycast hit position when a trigger is activated.
    /// </summary>
    public class ARInteractorSpawnTrigger : MonoBehaviour
    {
        /// <summary>
        ///     The type of trigger to use to spawn an object.
        /// </summary>
        public enum SpawnTriggerType
        {
            /// <summary>
            ///     Spawn an object when the interactor activates its select input
            ///     but no selection actually occurs.
            /// </summary>
            SelectAttempt,

            /// <summary>
            ///     Spawn an object when an input is performed.
            /// </summary>
            InputAction
        }

        [SerializeField] [Tooltip("The AR ray interactor that determines where to spawn the object.")]
        private XRRayInteractor m_ARInteractor;

        [SerializeField] [Tooltip("The behavior to use to spawn objects.")]
        private ObjectSpawner m_ObjectSpawner;

        [SerializeField]
        [Tooltip(
            "Whether to require that the AR Interactor hits an AR Plane with a horizontal up alignment in order to spawn anything.")]
        private bool m_RequireHorizontalUpSurface;

        [SerializeField]
        [Tooltip(
            "The type of trigger to use to spawn an object, either when the Interactor's select action occurs or " +
            "when a button input is performed.")]
        private SpawnTriggerType m_SpawnTriggerType;

        [SerializeField] private XRInputButtonReader m_SpawnObjectInput = new("Spawn Object");

        [SerializeField] [Tooltip("When enabled, spawn will not be triggered if an object is currently selected.")]
        private bool m_BlockSpawnWhenInteractorHasSelection = true;

        private bool m_AttemptSpawn;
        private bool m_EverHadSelection;

        /// <summary>
        ///     The AR ray interactor that determines where to spawn the object.
        /// </summary>
        public XRRayInteractor arInteractor
        {
            get => m_ARInteractor;
            set => m_ARInteractor = value;
        }

        /// <summary>
        ///     The behavior to use to spawn objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        /// <summary>
        ///     Whether to require that the <see cref="IARInteractor" /> hits an <see cref="ARPlane" /> with an alignment of
        ///     <see cref="PlaneAlignment.HorizontalUp" /> in order to spawn anything.
        /// </summary>
        public bool requireHorizontalUpSurface
        {
            get => m_RequireHorizontalUpSurface;
            set => m_RequireHorizontalUpSurface = value;
        }

        /// <summary>
        ///     The type of trigger to use to spawn an object.
        /// </summary>
        public SpawnTriggerType spawnTriggerType
        {
            get => m_SpawnTriggerType;
            set => m_SpawnTriggerType = value;
        }

        /// <summary>
        ///     The input used to trigger spawn, if <see cref="spawnTriggerType" /> is set to
        ///     <see cref="SpawnTriggerType.InputAction" />.
        /// </summary>
        public XRInputButtonReader spawnObjectInput
        {
            get => m_SpawnObjectInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_SpawnObjectInput, value, this);
        }

        /// <summary>
        ///     When enabled, spawn will not be triggered if an object is currently selected.
        /// </summary>
        public bool blockSpawnWhenInteractorHasSelection
        {
            get => m_BlockSpawnWhenInteractorHasSelection;
            set => m_BlockSpawnWhenInteractorHasSelection = value;
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void Start()
        {
            if (m_ObjectSpawner == null)
#if UNITY_2023_1_OR_NEWER
                m_ObjectSpawner = FindAnyObjectByType<ObjectSpawner>();
#else
                m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
#endif

            if (m_ARInteractor == null)
            {
                Debug.LogError("Missing AR Interactor reference, disabling component.", this);
                enabled = false;
            }
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void Update()
        {
            // Wait a frame after the Spawn Object input is triggered to actually cast against AR planes and spawn
            // in order to ensure the touchscreen gestures have finished processing to allow the ray pose driver
            // to update the pose based on the touch position of the gestures.
            if (m_AttemptSpawn)
            {
                m_AttemptSpawn = false;

                // Don't spawn the object if the tap was over screen space UI.
                var isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
                if (!isPointerOverUI && m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
                {
                    if (!(arRaycastHit.trackable is ARPlane arPlane))
                        return;

                    if (m_RequireHorizontalUpSurface && arPlane.alignment != PlaneAlignment.HorizontalUp)
                        return;

                    m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal);
                }

                return;
            }

            var selectState = m_ARInteractor.logicalSelectState;

            if (m_BlockSpawnWhenInteractorHasSelection)
            {
                if (selectState.wasPerformedThisFrame)
                    m_EverHadSelection = m_ARInteractor.hasSelection;
                else if (selectState.active)
                    m_EverHadSelection |= m_ARInteractor.hasSelection;
            }

            m_AttemptSpawn = false;
            switch (m_SpawnTriggerType)
            {
                case SpawnTriggerType.SelectAttempt:
                    if (selectState.wasCompletedThisFrame)
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                    break;

                case SpawnTriggerType.InputAction:
                    if (m_SpawnObjectInput.ReadWasPerformedThisFrame())
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                    break;
            }
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void OnEnable()
        {
            m_SpawnObjectInput.EnableDirectActionIfModeUsed();
        }

        /// <summary>
        ///     See <see cref="MonoBehaviour" />.
        /// </summary>
        private void OnDisable()
        {
            m_SpawnObjectInput.DisableDirectActionIfModeUsed();
        }
    }
}
#endif