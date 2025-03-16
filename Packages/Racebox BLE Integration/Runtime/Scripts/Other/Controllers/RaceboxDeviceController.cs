using System;
using System.Text;
using Android.BLE;
using Android.BLE.Commands;
using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
#endif

namespace RaceboxIntegration.Other
{
    public class RaceboxDeviceController : IDeviceController
    {
        private byte[] buffer = new byte[512];
        private int bufferPos;

        private SubscribeToCharacteristic readCommand;
        public string Firmware { get; private set; }
        public BluetoothDevice Device { get; private set; }
        public RaceboxData Data { get; private set; }

        public void Execute(BluetoothDevice device)
        {
            Device = device;
#if UNITY_EDITOR
            SimulateRaceboxData();
#else
            SubscribeToPrimaryUART();
            CheckFirmware();
            SetOptimalGNSSConfiguration(); // Add this line to configure GNSS on connection
#endif
        }

        public void SimulateRaceboxData()
        {
            Data = new RaceboxData
            {
                iTOW = (uint)Random.Range(0, int.MaxValue),
                year = (ushort)Random.Range(2000, 2030),
                month = (byte)Random.Range(1, 12),
                day = (byte)Random.Range(1, 28),
                hour = (byte)Random.Range(0, 23),
                minute = (byte)Random.Range(0, 59),
                second = (byte)Random.Range(0, 59),
                validityFlags = (byte)Random.Range(0, 255),
                timeAccuracy = (uint)Random.Range(0, int.MaxValue),
                nanoseconds = Random.Range(0, int.MaxValue),
                fixStatus = (byte)Random.Range(0, 3),
                fixStatusFlags = (byte)Random.Range(0, 255),
                dateTimeFlags = (byte)Random.Range(0, 255),
                numSVs = (byte)Random.Range(0, 255),
                longitude = Random.Range(-1800000000, 1800000000),
                latitude = Random.Range(-900000000, 900000000),
                wgsAltitude = Random.Range(-10000, 10000),
                mslAltitude = Random.Range(-10000, 10000),
                horizontalAccuracy = (uint)Random.Range(0, int.MaxValue),
                verticalAccuracy = (uint)Random.Range(0, int.MaxValue),
                speed = Random.Range(0, 100000),
                heading = Random.Range(0, 3600000),
                speedAccuracy = (uint)Random.Range(0, int.MaxValue),
                headingAccuracy = (uint)Random.Range(0, int.MaxValue),
                pdop = (ushort)Random.Range(0, 1000),
                latLonFlags = (byte)Random.Range(0, 255),
                batteryStatus = (byte)Random.Range(0, 255),
                gForceX = (short)Random.Range(short.MinValue, short.MaxValue),
                gForceY = (short)Random.Range(short.MinValue, short.MaxValue),
                gForceZ = (short)Random.Range(short.MinValue, short.MaxValue),
                rotRateX = (short)Random.Range(short.MinValue, short.MaxValue),
                rotRateY = (short)Random.Range(short.MinValue, short.MaxValue),
                rotRateZ = (short)Random.Range(short.MinValue, short.MaxValue)
            };

            MainEventBus.OnDeviceUpdated?.Invoke(Device.DeviceUID);
            //Debug.Log(GetOutput());
        }

        private void SubscribeToPrimaryUART()
        {
            Debug.Log("Subscribing to primary UART on " + Device.DeviceUID + " [" + Device.DeviceName + "]");
            readCommand = new SubscribeToCharacteristic(
                Device.DeviceUID,
                "6e400001-b5a3-f393-e0a9-e50e24dcca9e",
                "6e400003-b5a3-f393-e0a9-e50e24dcca9e",
                OnDataReceived,
                true
            );
            BleManager.Instance.QueueCommand(readCommand);
        }

        private void CheckFirmware()
        {
            Debug.Log("Checking Firmware for " + Device.DeviceUID + " [" + Device.DeviceName + "]");
            BleManager.Instance.QueueCommand(new ReadFromCharacteristic(
                Device.DeviceUID,
                "0000180a-0000-1000-8000-00805f9b34fb",
                "00002a26-0000-1000-8000-00805f9b34fb",
                value =>
                {
                    Firmware = Encoding.UTF8.GetString(value);
                    Debug.Log("Firmware: " + Firmware);
                },
                true
            ));
        }

        public void SetGNSSConfiguration(byte platformModel, bool enable3DSpeed, byte minAccuracy)
        {
            // Check if the device is connected
            if (!Device.IsConnected)
            {
                Debug.LogError("Cannot set GNSS configuration: Device is not connected.");
                return;
            }

            // Construct the payload
            var payload = new byte[3];
            payload[0] = platformModel; // Dynamic Platform Model (e.g., 4 for Automotive)
            payload[1] = (byte)(enable3DSpeed ? 1 : 0); // Enable 3D-Speed Reporting (0 or 1)
            payload[2] = minAccuracy; // Minimum Horizontal Accuracy (in 0.1m units)

            // Build the UBX packet
            var packet = BuildUBXPacket(0xFF, 0x27, payload);

            // Define UUIDs for the Racebox UART service
            var uartServiceUuid = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
            var rxCharacteristicUuid = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";

            // Queue the WriteToCharacteristic command with Base64-encoded data
            BleManager.Instance.QueueCommand(new WriteToCharacteristic(
                Device.DeviceUID, // Device address
                uartServiceUuid, // Service UUID
                rxCharacteristicUuid, // Characteristic UUID
                Convert.ToBase64String(packet), // Base64-encoded UBX packet
                true // Use customGatt to ensure Base64 encoding
            ));

            Debug.Log("Sent GNSS configuration command.");
        }

        /// <summary>
        ///     Sets the optimal GNSS configuration for the Racebox device (Automotive, ground speed, 1.0m accuracy).
        /// </summary>
        public void SetOptimalGNSSConfiguration()
        {
            SetGNSSConfiguration(4, false, 0); // Automotive, ground speed, 1.0m accuracy
        }

        private byte[] BuildUBXPacket(byte classId, byte messageId, byte[] payload)
        {
            var payloadLength = payload != null ? payload.Length : 0;
            var packet = new byte[8 + payloadLength]; // Header (2) + Class/ID (2) + Length (2) + Payload + Checksum (2)

            // Header
            packet[0] = 0xB5; // UBX sync character 1
            packet[1] = 0x62; // UBX sync character 2

            // Class and ID
            packet[2] = classId; // 0xFF for Racebox-specific messages
            packet[3] = messageId; // 0x27 for GNSS Receiver Configuration

            // Length (little-endian)
            packet[4] = (byte)(payloadLength & 0xFF);
            packet[5] = (byte)((payloadLength >> 8) & 0xFF);

            // Payload
            if (payload != null) Array.Copy(payload, 0, packet, 6, payloadLength);

            // Calculate checksum (CK_A and CK_B)
            byte ckA = 0;
            byte ckB = 0;
            for (var i = 2; i < 6 + payloadLength; i++)
            {
                ckA += packet[i];
                ckB += ckA;
            }

            packet[6 + payloadLength] = ckA;
            packet[7 + payloadLength] = ckB;

            return packet;
        }

        private void OnDataReceived(byte[] value)
        {
            if (value == null)
            {
#if UNITY_EDITOR
                SimulateRaceboxData();
#endif
                return;
            }

            // Ensure the buffer has enough space to accommodate the new data
            if (bufferPos + value.Length > buffer.Length) Array.Resize(ref buffer, bufferPos + value.Length);

            Array.Copy(value, 0, buffer, bufferPos, value.Length);
            bufferPos += value.Length;

            while (bufferPos >= 88)
                if (buffer[0] == 0xB5 && buffer[1] == 0x62)
                {
                    var length = BitConverter.ToUInt16(buffer, 4);
                    if (length == 80 && buffer[2] == 0xFF && buffer[3] == 0x01)
                    {
                        byte ckA = 0, ckB = 0;
                        for (var i = 2; i < 86; i++)
                        {
                            ckA += buffer[i];
                            ckB += ckA;
                        }

                        if (ckA == buffer[86] && ckB == buffer[87])
                        {
                            var packet = new byte[86];
                            Array.Copy(buffer, 0, packet, 0, 86);
                            ParseRaceBoxData(packet);
                            ShiftBuffer(88);
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
                    var shift = 1;
                    while (shift < bufferPos && (buffer[shift] != 0xB5 || buffer[shift + 1] != 0x62))
                        shift++;
                    ShiftBuffer(shift);
                }
        }

        private void ShiftBuffer(int amount)
        {
            Array.Copy(buffer, amount, buffer, 0, bufferPos - amount);
            bufferPos -= amount;
        }

        private void ParseRaceBoxData(byte[] packet)
        {
            if (Data == null)
                Data = new RaceboxData();
            Data.iTOW = BitConverter.ToUInt32(packet, 6);
            Data.year = BitConverter.ToUInt16(packet, 10);
            Data.month = packet[12];
            Data.day = packet[13];
            Data.hour = packet[14];
            Data.minute = packet[15];
            Data.second = packet[16];
            Data.validityFlags = packet[17];
            Data.timeAccuracy = BitConverter.ToUInt32(packet, 18);
            Data.nanoseconds = BitConverter.ToInt32(packet, 22);
            Data.fixStatus = packet[26];
            Data.fixStatusFlags = packet[27];
            Data.dateTimeFlags = packet[28];
            Data.numSVs = packet[29];
            Data.longitude = BitConverter.ToInt32(packet, 30) / 1e7; // Convert to double
            Data.latitude = BitConverter.ToInt32(packet, 34) / 1e7; // Convert to double
            Data.wgsAltitude = BitConverter.ToInt32(packet, 38);
            Data.mslAltitude = BitConverter.ToInt32(packet, 42);
            Data.horizontalAccuracy = BitConverter.ToUInt32(packet, 46);
            Data.verticalAccuracy = BitConverter.ToUInt32(packet, 50);
            Data.speed = BitConverter.ToInt32(packet, 54);
            Data.heading = BitConverter.ToInt32(packet, 58);
            Data.speedAccuracy = BitConverter.ToUInt32(packet, 62);
            Data.headingAccuracy = BitConverter.ToUInt32(packet, 66);
            Data.pdop = BitConverter.ToUInt16(packet, 70);
            Data.latLonFlags = packet[72];
            Data.batteryStatus = packet[73];
            Data.gForceX = BitConverter.ToInt16(packet, 74);
            Data.gForceY = BitConverter.ToInt16(packet, 76);
            Data.gForceZ = BitConverter.ToInt16(packet, 78);
            Data.rotRateX = BitConverter.ToInt16(packet, 80);
            Data.rotRateY = BitConverter.ToInt16(packet, 82);
            Data.rotRateZ = BitConverter.ToInt16(packet, 84);

            Data.timestamp = $"{Data.year}-{Data.month:00}-{Data.day:00} " +
                             $"{Data.hour:00}:{Data.minute:00}:{Data.second:00}.{Data.nanoseconds:000000000}";
            Data.validDate = (Data.validityFlags & 0x01) != 0;
            Data.validTime = (Data.validityFlags & 0x02) != 0;
            Data.fullyResolved = (Data.validityFlags & 0x04) != 0;
            Data.validMagDecl = (Data.validityFlags & 0x08) != 0;
            Data.validFix = (Data.fixStatusFlags & 0x01) != 0;
            Data.diffCorrApplied = (Data.fixStatusFlags & 0x02) != 0;
            Data.powerState = (Data.fixStatusFlags >> 2) & 0x07;
            Data.validHeading = (Data.fixStatusFlags & 0x20) != 0;
            Data.carrierPhase = (Data.fixStatusFlags >> 6) & 0x03;
            Data.dateTimeConfirmed = (Data.dateTimeFlags & 0x20) != 0;
            Data.dateValidConfirmed = (Data.dateTimeFlags & 0x40) != 0;
            Data.timeValidConfirmed = (Data.dateTimeFlags & 0x80) != 0;
            Data.invalidLatLon = (Data.latLonFlags & 0x01) != 0;
            Data.diffCorrAge = (Data.latLonFlags >> 1) & 0x0F;
            Data.isCharging = (Data.batteryStatus & 0x80) != 0;
            Data.batteryPercent = Data.batteryStatus & 0x7F;

            MainEventBus.OnDeviceUpdated?.Invoke(Device.DeviceUID);
            //Debug.Log(GetOutput());
        }

        public string GetOutput()
        {
            return $"RaceBox Data [{Device.DeviceName}]:\n" +
                   $"iTOW: {Data.iTOW} ms\n" +
                   $"Timestamp: {Data.timestamp} UTC\n" +
                   $"Validity Flags: Date={Data.validDate}, Time={Data.validTime}, Resolved={Data.fullyResolved}, MagDecl={Data.validMagDecl}\n" +
                   $"Time Accuracy: {Data.timeAccuracy} ns\n" +
                   $"Fix Status: {Data.fixStatus} (0=no fix, 2=2D, 3=3D)\n" +
                   $"Fix Status Flags: ValidFix={Data.validFix}, DiffCorr={Data.diffCorrApplied}, PowerState={Data.powerState}, Heading={Data.validHeading}, CarrierPhase={Data.carrierPhase}\n" +
                   $"Date/Time Flags: Confirmed={Data.dateTimeConfirmed}, DateValid={Data.dateValidConfirmed}, TimeValid={Data.timeValidConfirmed}\n" +
                   $"Number of SVs: {Data.numSVs}\n" +
                   $"Longitude: {Data.longitude} degrees\n" +
                   $"Latitude: {Data.latitude} degrees\n" +
                   $"WGS Altitude: {Data.wgsAltitude / 1000f} m\n" +
                   $"MSL Altitude: {Data.mslAltitude / 1000f} m\n" +
                   $"Horizontal Accuracy: {Data.horizontalAccuracy / 1000f} m\n" +
                   $"Vertical Accuracy: {Data.verticalAccuracy / 1000f} m\n" +
                   $"Speed: {Data.speed / 1000f} m/s\n" +
                   $"Heading: {Data.heading / 1e5f} degrees\n" +
                   $"Speed Accuracy: {Data.speedAccuracy / 1000f} m/s\n" +
                   $"Heading Accuracy: {Data.headingAccuracy / 1e5f} degrees\n" +
                   $"PDOP: {Data.pdop / 100f}\n" +
                   $"Lat/Lon Flags: Invalid={Data.invalidLatLon}, DiffCorrAge={Data.diffCorrAge}\n" +
                   $"Battery: {(Data.isCharging ? "Charging, " : "")}{Data.batteryPercent}% (Mini/Mini S) or Voltage: {Data.batteryStatus / 10f} V (Micro)\n" +
                   $"G-Force X: {Data.gForceX / 1000f} g\n" +
                   $"G-Force Y: {Data.gForceY / 1000f} g\n" +
                   $"G-Force Z: {Data.gForceZ / 1000f} g\n" +
                   $"Rotation Rate X: {Data.rotRateX / 100f} degrees per second\n" +
                   $"Rotation Rate Y: {Data.rotRateY / 100f} degrees per second\n" +
                   $"Rotation Rate Z: {Data.rotRateZ / 100f} degrees per second";
        }
    }
}