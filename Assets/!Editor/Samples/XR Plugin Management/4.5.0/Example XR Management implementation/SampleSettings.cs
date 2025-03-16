using System;
using UnityEngine;

namespace Samples
{
    /// <summary>
    ///     Simple sample settings showing how to create custom configuration data for your package.
    /// </summary>
    // Uncomment below line to have the settings appear in unified settings.
    //[XRConfigurationData("Sample Settings", SampleConstants.k_SettingsKey)]
    [Serializable]
    public class SampleSettings : ScriptableObject
    {
#if !UNITY_EDITOR
        /// <summary>Static instance that will hold the runtime asset instance we created in our build process.</summary>
        /// <see cref="SampleBuildProcessor"/>
        public static SampleSettings s_RuntimeInstance = null;
#endif

        /// <summary>Requirement settings enumeration</summary>
        public enum Requirement
        {
            /// <summary>Required</summary>
            Required,

            /// <summary>Optional</summary>
            Optional,

            /// <summary>None</summary>
            None
        }

        [SerializeField] [Tooltip("Changes item requirement.")]
        private Requirement m_RequiresItem;

        /// <summary>Whether or not the item is required.</summary>
        public Requirement requiresItem
        {
            get => m_RequiresItem;
            set => m_RequiresItem = value;
        }

        [SerializeField] [Tooltip("Some toggle for runtime.")]
        private bool m_RuntimeToggle = true;

        /// <summary>Where we toggled?</summary>
        public bool runtimeToggle
        {
            get => m_RuntimeToggle;
            set => m_RuntimeToggle = value;
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            s_RuntimeInstance = this;
#endif
        }
    }
}