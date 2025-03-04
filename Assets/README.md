# Bluetooth Manager for Unity

This Unity project provides a UI-based Bluetooth Low Energy (BLE) manager to scan for, connect to, and read data from BLE devices, specifically targeting RaceBox Mini devices.

## Critical Issue: Missing Native Library

The application is currently failing with this error:
```
dlopen failed: library "libunityandroidble.so" not found
```

This means the native BLE library is not properly included in your build. Follow the detailed steps below to fix this.

## Requirements

- Unity 2020.3 or newer
- Android 6.0+ for BLE functionality
- Android.BLE plugin (must be properly imported)

## Step-by-Step Setup Instructions

### 1. Import the Android.BLE Plugin

First, ensure the Android.BLE plugin is imported into your project:
1. Download the plugin from the Unity Asset Store or plugin provider
2. Import it into your Unity project
3. Verify the plugin files are in your Assets folder

### 2. Add the Native Libraries

The **most critical step** is ensuring the native libraries are in the correct location:

1. Create the following folder structure if it doesn't exist:
   ```
   Assets/Plugins/Android/
   ```

2. Place the native library file in this folder:
   ```
   Assets/Plugins/Android/libunityandroidble.so
   ```

3. If you have architecture-specific libraries, organize them as follows:
   ```
   Assets/Plugins/Android/libs/arm64-v8a/libunityandroidble.so
   Assets/Plugins/Android/libs/armeabi-v7a/libunityandroidble.so
   ```

4. IMPORTANT: If you don't have these files, you need to:
   - Contact the plugin provider for the native libraries
   - Check if they're included in a different location in the plugin package
   - Ensure you have the correct version of the plugin for your Unity version

### 3. Configure Build Settings

1. Set platform to Android in Build Settings
2. In Player Settings > Other Settings:
   - Set API Level to Android 6.0 (API 23) or higher
   - Enable these permissions:
     - `External Calls`
     - `Bluetooth`
     - `Bluetooth Admin`
     - `Access Fine Location` (required for BLE scanning)

### 4. Configure Gradle Templates

If the library still isn't found after adding it to the correct location:

1. In Project Settings > Player > Publishing Settings, enable "Custom Gradle Template"
2. Edit the mainTemplate.gradle file to include:

```gradle
android {
    // Other settings...
    
    packagingOptions {
        doNotStrip '*/armeabi-v7a/*.so'
        doNotStrip '*/arm64-v8a/*.so'
    }
    
    // Ensure the .so files are included
    sourceSets {
        main {
            jniLibs.srcDirs = ['../Plugins/Android/libs']
        }
    }
}
```

## Verifying the Library

After building your app, you can check if the library was included correctly:

1. Install the app on your device
2. Check logcat for the following message:
   ```
   dlopen failed: library "libunityandroidble.so" not found
   ```
   If you see this, the library is still missing from your build

3. Use our NativeLibraryChecker that will search for the library in these locations:
   ```
   /data/app/YOUR_PACKAGE_NAME/lib/arm64/libunityandroidble.so
   /data/app/YOUR_PACKAGE_NAME/lib/arm64-v8a/libunityandroidble.so
   /data/app/YOUR_PACKAGE_NAME/libs/arm64-v8a/libunityandroidble.so
   /data/data/YOUR_PACKAGE_NAME/lib/libunityandroidble.so
   /data/data/YOUR_PACKAGE_NAME/libs/libunityandroidble.so
   ```

## Troubleshooting

If Bluetooth is only simulating connections but not making real ones:

1. **Missing Library**: The app will fall back to simulation mode if it can't find the native library
   - Check Plugins folder structure
   - Verify library names are correct (case-sensitive)
   - Rebuild the app after adding the libraries

2. **Permission Issues**:
   - Ensure all required permissions are granted on the device
   - For Android 6.0+, runtime permissions must be accepted by the user

3. **Plugin Compatibility**:
   - Verify the plugin is compatible with your Unity version
   - Check if there are any updates available for the plugin

## License

This project is licensed under the MIT License. 