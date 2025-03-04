using System;
using System.Text;
using Android.BLE;
using Android.BLE.Commands;
using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using UnityEngine;

namespace RaceboxIntegration.Other
{
    /// <summary>
    /// Controller responsible for managing Racebox device communication and data parsing.
    /// Implements the Racebox protocol specification for GPS/IMU data streaming.
    /// Handles device connection, firmware checking, and real-time data processing.
    /// </summary>
    public class RaceboxDeviceController : IDeviceController
    {

        public string Firmware { get; private set; }

        private BluetoothDevice device;
        private SubscribeToCharacteristic readCommand;
        
        private byte[] buffer = new byte[512]; 
        private int bufferPos = 0;

        // Field to store the latest parsed data
        private RaceboxData currentData = new RaceboxData();

        public void Execute(BluetoothDevice device)
        {
            this.device = device;
            SubscribeToPrimaryUART();
            CheckFirmware();
        }

        private void SubscribeToPrimaryUART()
        {
            Debug.Log("Subscribing to primary UART on " + device.DeviceUID + " [" + device.DeviceName + "]");
            readCommand = new SubscribeToCharacteristic(
                device.DeviceUID,
                "6e400001-b5a3-f393-e0a9-e50e24dcca9e",
                "6e400003-b5a3-f393-e0a9-e50e24dcca9e",
                OnDataReceived,
                true
            );
            BleManager.Instance.QueueCommand(readCommand);
        }

        private void CheckFirmware()
        {
            Debug.Log("Checking Firmware for " + device.DeviceUID + " [" + device.DeviceName + "]");
            BleManager.Instance.QueueCommand(new ReadFromCharacteristic(
                device.DeviceUID,
                "0000180a-0000-1000-8000-00805f9b34fb",
                "00002a26-0000-1000-8000-00805f9b34fb",
                (byte[] value) =>
                {
                    Firmware = Encoding.UTF8.GetString(value);
                    Debug.Log("Firmware: " + Firmware);
                },
                true
            ));
        }

        private void OnDataReceived(byte[] value)
        {
            Debug.Log("Raw Data: " + BitConverter.ToString(value));
            Array.Copy(value, 0, buffer, bufferPos, value.Length);
            bufferPos += value.Length;

            while (bufferPos >= 88) // Full packet size: 88 bytes
            {
                if (buffer[0] == 0xB5 && buffer[1] == 0x62)
                {
                    ushort length = BitConverter.ToUInt16(buffer, 4);
                    if (length == 80 && buffer[2] == 0xFF && buffer[3] == 0x01)
                    {
                        byte ckA = 0, ckB = 0;
                        for (int i = 2; i < 86; i++) // Bytes 2 to 85 (84 bytes)
                        {
                            ckA += buffer[i];
                            ckB += ckA;
                        }
                        if (ckA == buffer[86] && ckB == buffer[87]) // Checksum at 86 and 87
                        {
                            byte[] packet = new byte[86]; // Payload + header up to byte 85
                            Array.Copy(buffer, 0, packet, 0, 86);
                            ParseRaceBoxData(packet);
                            ShiftBuffer(88); // Shift full packet
                        }
                        else
                        {
                            Debug.LogWarning("Checksum invalid: " + BitConverter.ToString(buffer, 0, 88));
                            ShiftBuffer(1);
                        }
                    }
                    else
                    {
                        ShiftBuffer(1);
                    }
                }
                else
                {
                    int shift = 1;
                    while (shift < bufferPos && (buffer[shift] != 0xB5 || buffer[shift + 1] != 0x62))
                        shift++;
                    ShiftBuffer(shift);
                }
            }
        }

        private void ShiftBuffer(int amount)
        {
            Array.Copy(buffer, amount, buffer, 0, bufferPos - amount);
            bufferPos -= amount;
        }

        private void ParseRaceBoxData(byte[] packet)
        {
            // Extract all fields from the 80-byte payload and assign to currentData (Pages 4-6)
            currentData.iTOW = BitConverter.ToUInt32(packet, 6);
            currentData.year = BitConverter.ToUInt16(packet, 10);
            currentData.month = packet[12];
            currentData.day = packet[13];
            currentData.hour = packet[14];
            currentData.minute = packet[15];
            currentData.second = packet[16];
            currentData.validityFlags = packet[17];
            currentData.timeAccuracy = BitConverter.ToUInt32(packet, 18);
            currentData.nanoseconds = BitConverter.ToInt32(packet, 22);
            currentData.fixStatus = packet[26];
            currentData.fixStatusFlags = packet[27];
            currentData.dateTimeFlags = packet[28];
            currentData.numSVs = packet[29];
            currentData.longitude = BitConverter.ToInt32(packet, 30);
            currentData.latitude = BitConverter.ToInt32(packet, 34);
            currentData.wgsAltitude = BitConverter.ToInt32(packet, 38);
            currentData.mslAltitude = BitConverter.ToInt32(packet, 42);
            currentData.horizontalAccuracy = BitConverter.ToUInt32(packet, 46);
            currentData.verticalAccuracy = BitConverter.ToUInt32(packet, 50);
            currentData.speed = BitConverter.ToInt32(packet, 54);
            currentData.heading = BitConverter.ToInt32(packet, 58);
            currentData.speedAccuracy = BitConverter.ToUInt32(packet, 62);
            currentData.headingAccuracy = BitConverter.ToUInt32(packet, 66);
            currentData.pdop = BitConverter.ToUInt16(packet, 70);
            currentData.latLonFlags = packet[72];
            currentData.batteryStatus = packet[73];
            currentData.gForceX = BitConverter.ToInt16(packet, 74);
            currentData.gForceY = BitConverter.ToInt16(packet, 76);
            currentData.gForceZ = BitConverter.ToInt16(packet, 78);
            currentData.rotRateX = BitConverter.ToInt16(packet, 80);
            currentData.rotRateY = BitConverter.ToInt16(packet, 82);
            currentData.rotRateZ = BitConverter.ToInt16(packet, 84);

            // Apply scaling factors and decode flags (Pages 5-6)
            currentData.timestamp = $"{currentData.year}-{currentData.month:00}-{currentData.day:00} " +
                                    $"{currentData.hour:00}:{currentData.minute:00}:{currentData.second:00}.{currentData.nanoseconds:000000000}";
            currentData.validDate = (currentData.validityFlags & 0x01) != 0;
            currentData.validTime = (currentData.validityFlags & 0x02) != 0;
            currentData.fullyResolved = (currentData.validityFlags & 0x04) != 0;
            currentData.validMagDecl = (currentData.validityFlags & 0x08) != 0;
            currentData.validFix = (currentData.fixStatusFlags & 0x01) != 0;
            currentData.diffCorrApplied = (currentData.fixStatusFlags & 0x02) != 0;
            currentData.powerState = (currentData.fixStatusFlags >> 2) & 0x07;
            currentData.validHeading = (currentData.fixStatusFlags & 0x20) != 0;
            currentData.carrierPhase = (currentData.fixStatusFlags >> 6) & 0x03;
            currentData.dateTimeConfirmed = (currentData.dateTimeFlags & 0x20) != 0;
            currentData.dateValidConfirmed = (currentData.dateTimeFlags & 0x40) != 0;
            currentData.timeValidConfirmed = (currentData.dateTimeFlags & 0x80) != 0;
            currentData.invalidLatLon = (currentData.latLonFlags & 0x01) != 0;
            currentData.diffCorrAge = (currentData.latLonFlags >> 1) & 0x0F;
            currentData.isCharging = (currentData.batteryStatus & 0x80) != 0;
            currentData.batteryPercent = currentData.batteryStatus & 0x7F; // For Mini/Mini S; Micro uses voltage

            MainEventBus.OnDeviceRefreshed?.Invoke(device.DeviceUID);
            
            // Log all data with units (Pages 6-7)
            Debug.Log(GetOutput());
        }

        public string GetOutput()
        {
            return $"RaceBox Data [{device.DeviceName}]:\n" +
                   $"iTOW: {currentData.iTOW} ms\n" +
                   $"Timestamp: {currentData.timestamp} UTC\n" +
                   $"Validity Flags: Date={currentData.validDate}, Time={currentData.validTime}, Resolved={currentData.fullyResolved}, MagDecl={currentData.validMagDecl}\n" +
                   $"Time Accuracy: {currentData.timeAccuracy} ns\n" +
                   $"Fix Status: {currentData.fixStatus} (0=no fix, 2=2D, 3=3D)\n" +
                   $"Fix Status Flags: ValidFix={currentData.validFix}, DiffCorr={currentData.diffCorrApplied}, PowerState={currentData.powerState}, Heading={currentData.validHeading}, CarrierPhase={currentData.carrierPhase}\n" +
                   $"Date/Time Flags: Confirmed={currentData.dateTimeConfirmed}, DateValid={currentData.dateValidConfirmed}, TimeValid={currentData.timeValidConfirmed}\n" +
                   $"Number of SVs: {currentData.numSVs}\n" +
                   $"Longitude: {currentData.longitude / 1e7f} degrees\n" +
                   $"Latitude: {currentData.latitude / 1e7f} degrees\n" +
                   $"WGS Altitude: {currentData.wgsAltitude / 1000f} m\n" +
                   $"MSL Altitude: {currentData.mslAltitude / 1000f} m\n" +
                   $"Horizontal Accuracy: {currentData.horizontalAccuracy / 1000f} m\n" +
                   $"Vertical Accuracy: {currentData.verticalAccuracy / 1000f} m\n" +
                   $"Speed: {currentData.speed / 1000f} m/s\n" +
                   $"Heading: {currentData.heading / 1e5f} degrees\n" +
                   $"Speed Accuracy: {currentData.speedAccuracy / 1000f} m/s\n" +
                   $"Heading Accuracy: {currentData.headingAccuracy / 1e5f} degrees\n" +
                   $"PDOP: {currentData.pdop / 100f}\n" +
                   $"Lat/Lon Flags: Invalid={currentData.invalidLatLon}, DiffCorrAge={currentData.diffCorrAge}\n" +
                   $"Battery: {(currentData.isCharging ? "Charging, " : "")}{currentData.batteryPercent}% (Mini/Mini S) or Voltage: {currentData.batteryStatus / 10f} V (Micro)\n" +
                   $"G-Force X: {currentData.gForceX / 1000f} g\n" +
                   $"G-Force Y: {currentData.gForceY / 1000f} g\n" +
                   $"G-Force Z: {currentData.gForceZ / 1000f} g\n" +
                   $"Rotation Rate X: {currentData.rotRateX / 100f} degrees per second\n" +
                   $"Rotation Rate Y: {currentData.rotRateY / 100f} degrees per second\n" +
                   $"Rotation Rate Z: {currentData.rotRateZ / 100f} degrees per second";
        }
    }
}