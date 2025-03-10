#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class StartupCheckEditor
{
    private const string MenuItemName = "Tools/Enable Startup Check";
    private const string PrefKey = "StartupCheckEnabled";

    [MenuItem(MenuItemName)]
    private static void ToggleStartupCheck()
    {
        bool isEnabled = !IsStartupCheckEnabled();
        EditorPrefs.SetBool(PrefKey, isEnabled);
        Menu.SetChecked(MenuItemName, isEnabled);
    }

    [MenuItem(MenuItemName, true)]
    private static bool ValidateToggleStartupCheck()
    {
        Menu.SetChecked(MenuItemName, IsStartupCheckEnabled());
        return true;
    }

    public static bool IsStartupCheckEnabled()
    {
        return EditorPrefs.GetBool(PrefKey, true);
    }
}
#endif