using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public class AndroidManifestResolver : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (File.Exists(manifestPath))
            {
                var manifest = File.ReadAllText(manifestPath);
                Debug.Log("Original AndroidManifest.xml content:\n" + manifest);
                manifest = AddBluetoothPermissions(manifest);
                File.WriteAllText(manifestPath, manifest);
                Debug.Log("Updated AndroidManifest.xml content:\n" + manifest);
                Debug.Log("AndroidManifest.xml updated with Bluetooth permissions.");
            }
            else
            {
                Debug.LogError("AndroidManifest.xml not found.");
            }
        }
    }

    private string AddBluetoothPermissions(string manifest)
    {
        string permissions = @"
    <uses-permission android:name=""android.permission.BLUETOOTH""/>
    <uses-permission android:name=""android.permission.BLUETOOTH_ADMIN""/>
    <uses-permission android:name=""android.permission.BLUETOOTH_SCAN"" android:usesPermissionFlags=""neverForLocation""/>
    <uses-permission android:name=""android.permission.BLUETOOTH_CONNECT""/>
    <uses-permission android:name=""android.permission.ACCESS_FINE_LOCATION""/>
";
        if (!manifest.Contains("android.permission.BLUETOOTH"))
        {
            int insertIndex = manifest.IndexOf("<application");
            if (insertIndex != -1)
            {
                manifest = manifest.Insert(insertIndex, permissions);
            }
            else
            {
                Debug.LogError("<application> tag not found in AndroidManifest.xml.");
            }
        }
        return manifest;
    }
}