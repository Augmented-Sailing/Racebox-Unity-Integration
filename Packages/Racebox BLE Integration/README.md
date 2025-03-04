# Racebox Bluetooth Integration for Unity

A robust Unity implementation for integrating Racebox GPS/IMU devices via Bluetooth Low Energy (BLE). This package provides a complete solution for connecting to, reading data from, and displaying information from Racebox devices in Unity applications.

## Features

- **Device Discovery**: Automatic scanning and discovery of Racebox devices
- **Real-time Data Streaming**: Continuous monitoring of GPS, IMU, and device status data
- **Event-Driven Architecture**: Clean event system for handling device state changes and data updates
- **UI Components**: Pre-built UI system for device management and data visualization
- **Cross-Platform Support**: Built for Android with Unity Editor simulation support

## Technical Requirements

- Unity 2020.3 or later
- Android API Level 21 or later
- Android.BLE plugin (included in package)

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

## Usage

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

- Editor simulation mode is supported for testing without physical devices
- All BLE operations are handled asynchronously
- Device data is parsed and validated before distribution
- UI components are designed for easy customization

## Best Practices

1. Always check device connection state before accessing data
2. Implement proper error handling for BLE operations
3. Use the event system for UI updates rather than direct polling
4. Clean up event listeners when components are disabled

## License

[Your License Here]

## Support

For issues and feature requests, please use the project's issue tracker.