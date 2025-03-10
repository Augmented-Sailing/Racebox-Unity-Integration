using System;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Android.BLE;
using Android.BLE.Commands;
using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using UnityEngine;

namespace RaceboxIntegration.Other
{
    public class RaceboxDeviceController : IDeviceController
    {
        public string Firmware { get; private set; }
        public BluetoothDevice Device { get; private set; }
        public RaceboxData Data { get; private set; }
        
        private SubscribeToCharacteristic readCommand;
        
        private byte[] buffer = new byte[512]; 
        private int bufferPos = 0;

        public void Execute(BluetoothDevice device)
        {
            this.Device = device;
#if UNITY_EDITOR
            SimulateRaceboxData();
#else
            SubscribeToPrimaryUART();
            CheckFirmware();
#endif
        }

        public void SimulateRaceboxData()
        {
            Data = new RaceboxData
            {
                iTOW = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                year = (ushort)UnityEngine.Random.Range(2000, 2030),
                month = (byte)UnityEngine.Random.Range(1, 12),
                day = (byte)UnityEngine.Random.Range(1, 28),
                hour = (byte)UnityEngine.Random.Range(0, 23),
                minute = (byte)UnityEngine.Random.Range(0, 59),
                second = (byte)UnityEngine.Random.Range(0, 59),
                validityFlags = (byte)UnityEngine.Random.Range(0, 255),
                timeAccuracy = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                nanoseconds = UnityEngine.Random.Range(0, int.MaxValue),
                fixStatus = (byte)UnityEngine.Random.Range(0, 3),
                fixStatusFlags = (byte)UnityEngine.Random.Range(0, 255),
                dateTimeFlags = (byte)UnityEngine.Random.Range(0, 255),
                numSVs = (byte)UnityEngine.Random.Range(0, 255),
                longitude = UnityEngine.Random.Range(-1800000000, 1800000000),
                latitude = UnityEngine.Random.Range(-900000000, 900000000),
                wgsAltitude = UnityEngine.Random.Range(-10000, 10000),
                mslAltitude = UnityEngine.Random.Range(-10000, 10000),
                horizontalAccuracy = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                verticalAccuracy = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                speed = UnityEngine.Random.Range(0, 100000),
                heading = UnityEngine.Random.Range(0, 3600000),
                speedAccuracy = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                headingAccuracy = (uint)UnityEngine.Random.Range(0, int.MaxValue),
                pdop = (ushort)UnityEngine.Random.Range(0, 1000),
                latLonFlags = (byte)UnityEngine.Random.Range(0, 255),
                batteryStatus = (byte)UnityEngine.Random.Range(0, 255),
                gForceX = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                gForceY = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                gForceZ = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                rotRateX = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                rotRateY = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                rotRateZ = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue)
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
            if (value == null)
            {
#if UNITY_EDITOR
                SimulateRaceboxData();
#endif
            }
            //Debug.Log("Raw Data: " + BitConverter.ToString(value));
            Array.Copy(value, 0, buffer, bufferPos, value.Length);
            bufferPos += value.Length;

            while (bufferPos >= 88)
            {
                if (buffer[0] == 0xB5 && buffer[1] == 0x62)
                {
                    ushort length = BitConverter.ToUInt16(buffer, 4);
                    if (length == 80 && buffer[2] == 0xFF && buffer[3] == 0x01)
                    {
                        byte ckA = 0, ckB = 0;
                        for (int i = 2; i < 86; i++)
                        {
                            ckA += buffer[i];
                            ckB += ckA;
                        }
                        if (ckA == buffer[86] && ckB == buffer[87])
                        {
                            byte[] packet = new byte[86];
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
            Data.longitude = BitConverter.ToInt32(packet, 30);
            Data.latitude = BitConverter.ToInt32(packet, 34);
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
                   $"Longitude: {Data.longitude / 1e7f} degrees\n" +
                   $"Latitude: {Data.latitude / 1e7f} degrees\n" +
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