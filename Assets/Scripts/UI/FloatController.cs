using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Globalization;
using TMPro;

public class FloatController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Slider slider;
    [SerializeField] private string format = "F2";

    public UnityEvent<float> onValueChanged;

    private float currentValue;
    private float minValue = 0f;
    private float maxValue = 1f;

    private void Awake()
    {
        // Ensure references are assigned
        if (inputField == null || slider == null)
        {
            Debug.LogError("InputField or Slider is not assigned in FloatController.", this);
            return;
        }

        // Set input field to accept decimal numbers
        inputField.contentType = TMP_InputField.ContentType.DecimalNumber;

        // Add event listeners
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);

        // Initialize with slider's value
        currentValue = slider.value;
        UpdateUI();
    }

    /// <summary>
    /// Gets or sets the current float value.
    /// </summary>
    public float Value
    {
        get { return currentValue; }
        set { SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public float MinValue
    {
        get { return minValue; }
        set
        {
            minValue = value;
            slider.minValue = minValue;
            if (currentValue < minValue)
            {
                SetValue(minValue);
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public float MaxValue
    {
        get { return maxValue; }
        set
        {
            maxValue = value;
            slider.maxValue = maxValue;
            if (currentValue > maxValue)
            {
                SetValue(maxValue);
            }
        }
    }

    /// <summary>
    /// Sets the value, updates UI, and invokes the change event if the value differs.
    /// </summary>
    private void SetValue(float newValue)
    {
        if (currentValue != newValue)
        {
            currentValue = Mathf.Clamp(newValue, minValue, maxValue);
            UpdateUI();
            onValueChanged?.Invoke(currentValue);
        }
    }

    /// <summary>
    /// Sets the value without notifying listeners.
    /// </summary>
    public void SetValueWithoutNotify(float newValue)
    {
        currentValue = Mathf.Clamp(newValue, minValue, maxValue);
        UpdateUI();
    }

    /// <summary>
    /// Updates the slider and input field to reflect the current value.
    /// </summary>
    private void UpdateUI()
    {
        slider.value = currentValue;
        inputField.text = currentValue.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Handles slider value changes.
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        SetValue(value);
    }

    /// <summary>
    /// Handles input field changes when editing ends.
    /// </summary>
    private void OnInputFieldEndEdit(string text)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
        {
            SetValue(newValue);
        }
        else
        {
            // Revert to current value if input is invalid
            inputField.text = currentValue.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}