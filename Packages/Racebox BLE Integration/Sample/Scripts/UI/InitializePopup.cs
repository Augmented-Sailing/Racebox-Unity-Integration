using System.Collections;
using Android.BLE;
using RaceboxIntegration.Events;
using TMPro;
using UnityEngine;

namespace RaceboxIntegration.UI
{
    /// <summary>
    ///     UI component that displays the initialization status of the BLE system.
    ///     Provides visual feedback during the BLE initialization process.
    /// </summary>
    public class InitializePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text detailText;

        private void OnEnable()
        {
            MainEventBus.OnBLEInitialized.AddListener(OnInitialized);
        }

        private void OnDisable()
        {
            MainEventBus.OnBLEInitialized.RemoveListener(OnInitialized);
        }

        /// <summary>
        ///     Displays the initialization popup and starts the auto-close sequence if already initialized.
        /// </summary>
        public void ShowPopup()
        {
            Refresh();

            gameObject.SetActive(true);

            if (BleManager.IsInitialized)
                StartCoroutine(AutomaticallyClose());
        }

        private void Refresh()
        {
            if (BleManager.IsInitialized)
                detailText.SetText("Initialized Bluetooth");
            else
                detailText.SetText("Initializing Bluetooth");
        }

        /// <summary>
        ///     Automatically closes the popup after a delay.
        /// </summary>
        private IEnumerator AutomaticallyClose()
        {
            yield return new WaitForSeconds(1.5f);
            gameObject.SetActive(false);
        }

        private void OnInitialized()
        {
            Refresh();
            StartCoroutine(AutomaticallyClose());
        }
    }
}