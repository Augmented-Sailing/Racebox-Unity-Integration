#if AR_FOUNDATION_PRESENT
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.AR.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    ///     Handles dismissing the object menu when clicking out the UI bounds, and showing the
    ///     menu again when the create menu button is clicked after dismissal. Manages object deletion in the AR demo scene,
    ///     and also handles the toggling between the object creation menu button and the delete button.
    /// </summary>
    public class ARSampleMenuManager : MonoBehaviour
    {
        [SerializeField] [Tooltip("Button that opens the create menu.")]
        private Button m_CreateButton;

        [SerializeField] [Tooltip("Button that deletes a selected object.")]
        private Button m_DeleteButton;

        [SerializeField] [Tooltip("The menu with all the creatable objects.")]
        private GameObject m_ObjectMenu;

        [SerializeField] [Tooltip("The animator for the object creation menu.")]
        private Animator m_ObjectMenuAnimator;

        [SerializeField] [Tooltip("The object spawner component in charge of spawning new objects.")]
        private ObjectSpawner m_ObjectSpawner;

        [SerializeField] [Tooltip("Button that closes the object creation menu.")]
        private Button m_CancelButton;

        [SerializeField] [Tooltip("The interaction group for the AR demo scene.")]
        private XRInteractionGroup m_InteractionGroup;

        [SerializeField] private XRInputValueReader<Vector2> m_TapStartPositionInput = new("Tap Start Position");

        private bool m_ShowObjectMenu;

        /// <summary>
        ///     Button that opens the create menu.
        /// </summary>
        public Button createButton
        {
            get => m_CreateButton;
            set => m_CreateButton = value;
        }

        /// <summary>
        ///     Button that deletes a selected object.
        /// </summary>
        public Button deleteButton
        {
            get => m_DeleteButton;
            set => m_DeleteButton = value;
        }

        /// <summary>
        ///     The menu with all the creatable objects.
        /// </summary>
        public GameObject objectMenu
        {
            get => m_ObjectMenu;
            set => m_ObjectMenu = value;
        }

        /// <summary>
        ///     The animator for the object creation menu.
        /// </summary>
        public Animator objectMenuAnimator
        {
            get => m_ObjectMenuAnimator;
            set => m_ObjectMenuAnimator = value;
        }

        /// <summary>
        ///     The object spawner component in charge of spawning new objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        /// <summary>
        ///     Button that closes the object creation menu.
        /// </summary>
        public Button cancelButton
        {
            get => m_CancelButton;
            set => m_CancelButton = value;
        }

        /// <summary>
        ///     The interaction group for the AR demo scene.
        /// </summary>
        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        /// <summary>
        ///     Input to use for the screen tap start position.
        /// </summary>
        /// <seealso cref="TouchscreenGestureInputController.tapStartPosition" />
        public XRInputValueReader<Vector2> tapStartPositionInput
        {
            get => m_TapStartPositionInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_TapStartPositionInput, value, this);
        }

        private void Start()
        {
            HideMenu();
        }

        private void Update()
        {
            if (m_ShowObjectMenu)
            {
                m_CreateButton.gameObject.SetActive(false);
                m_DeleteButton.gameObject.SetActive(false);
                var isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
                if (!isPointerOverUI && m_TapStartPositionInput.TryReadValue(out _)) HideMenu();
            }
            else if (m_InteractionGroup is not null)
            {
                var currentFocusedObject = m_InteractionGroup.focusInteractable;
                if (currentFocusedObject != null &&
                    (!m_DeleteButton.isActiveAndEnabled || m_CreateButton.isActiveAndEnabled))
                {
                    m_CreateButton.gameObject.SetActive(false);
                    m_DeleteButton.gameObject.SetActive(true);
                }
                else if (currentFocusedObject == null &&
                         (!m_CreateButton.isActiveAndEnabled || m_DeleteButton.isActiveAndEnabled))
                {
                    m_CreateButton.gameObject.SetActive(true);
                    m_DeleteButton.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            m_TapStartPositionInput.EnableDirectActionIfModeUsed();
            m_CreateButton.onClick.AddListener(ShowMenu);
            m_CancelButton.onClick.AddListener(HideMenu);
            m_DeleteButton.onClick.AddListener(DeleteFocusedObject);
        }

        private void OnDisable()
        {
            m_TapStartPositionInput.DisableDirectActionIfModeUsed();
            m_ShowObjectMenu = false;
            m_CreateButton.onClick.RemoveListener(ShowMenu);
            m_CancelButton.onClick.RemoveListener(HideMenu);
            m_DeleteButton.onClick.RemoveListener(DeleteFocusedObject);
        }

        public void SetObjectToSpawn(int objectIndex)
        {
            if (m_ObjectSpawner == null)
            {
                Debug.LogWarning("Menu Manager not configured correctly: no Object Spawner set.", this);
            }
            else
            {
                if (objectIndex < m_ObjectSpawner.objectPrefabs.Count)
                    m_ObjectSpawner.spawnOptionIndex = objectIndex;
                else
                    Debug.LogWarning(
                        "Object Spawner not configured correctly: object index larger than number of Object Prefabs.",
                        this);
            }

            HideMenu();
        }

        private void ShowMenu()
        {
            m_ShowObjectMenu = true;
            m_ObjectMenu.SetActive(true);
            if (!m_ObjectMenuAnimator.GetBool("Show")) m_ObjectMenuAnimator.SetBool("Show", true);
        }

        /// <summary>
        ///     Triggers hide animation for menu.
        /// </summary>
        public void HideMenu()
        {
            m_ObjectMenuAnimator.SetBool("Show", false);
            m_ShowObjectMenu = false;
        }

        private void DeleteFocusedObject()
        {
            if (m_InteractionGroup == null)
                return;

            var currentFocusedObject = m_InteractionGroup.focusInteractable;
            if (currentFocusedObject != null) Destroy(currentFocusedObject.transform.gameObject);
        }
    }
}
#endif