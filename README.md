# Racebox Bluetooth Integration for Unity
A Unity implementation for integrating Racebox GPS/IMU devices via Bluetooth Low Energy (BLE). This package provides a complete solution for connecting to, reading data from, and displaying information from Racebox devices in Unity applications.

## Functionality

https://github.com/user-attachments/assets/917ef5ba-952b-4ad9-91cb-63c09ab879e8


- Scan for and connect to Bluetooth Devices
- Parses Racebox specific data streams to read it's data

## Additional Features
- Sample setup for interacting with the Package
- AR Demo Project hooked up with Racebox's Data

## Technical Requirements
- Unity 2020.3 or later running on Android
- [Unity Android.BLE Plugin](https://github.com/Velorexe/Unity-Android-Bluetooth-Low-Energy)

## Usage
Import the package via PM
```
https://github.com/Augmented-Sailing/Racebox-Unity-Integration.git?path=/Packages/Racebox%20BLE%20Integration
```

1. Initialize the Bluetooth system:
```csharp
BleManager.Instance.Initialize();
```

2. Scan for devices:
```csharp
BluetoothManager.Instance.ScanForDevices();
```

3. Connect to a device:
```csharp
BluetoothManager.Instance.Connect(device);
```

4. Access device data through the `RaceboxDeviceController`:
```csharp
var controller = device.DeviceController as RaceboxDeviceController;
string data = controller.GetOutput();
```

## Architecture

### Core Components
- `BluetoothManager`: Central manager for BLE operations and device tracking
- `RaceboxDeviceController`: Handles Racebox-specific protocol implementation
- `BluetoothDevice`: Represents a discovered BLE device with connection state management
- `RaceboxData`: Data model for parsed Racebox sensor information

### Data Protocol
The implementation follows the Racebox protocol specification:
- Packet size: 88 bytes
- Header: 0xB5 0x62
- Payload: 80 bytes
- Checksum: 2 bytes

## Events
The system uses a centralized event bus (`MainEventBus`) for communication:

- `OnBLEInitialized`: Triggered when BLE system is ready
- `OnDeviceFound`: Fired when a new device is discovered
- `OnDeviceStatusUpdated`: Notifies of device connection state changes
- `OnDeviceRefreshed`: Indicates new data available from device

## UI Components
The package includes a complete UI system:

- `BluetoothUI`: Main UI controller
- `ConnectionComponent`: Device scanning and connection interface
- `DevicePopup`: Device details and actions
- `OutputComponent`: Real-time data display
- `InitializePopup`: BLE initialization status

## Development Notes
- Editor simulation mode has limited support for testing without physical devices
- There will need to be a system in place to automatically scan and pair to the device in the final solution.
  - Make sure to cancel the scan as soon as device is detected.
  - Make sure the device is recovered if connection is lost, with proper event handling in-game.

## References
- [Racebox Protocol Documentation](https://www.racebox.pro/products/mini-micro-protocol-documentation?k=67c166d0bda80de96505efba)
- [Geospatial Creator Documentation](https://developers.google.com/ar/geospatialcreator/unity/quickstart)

## Support
For issues and feature requests, please use the project's issue tracker, or reach out to sam@augmented.cool
