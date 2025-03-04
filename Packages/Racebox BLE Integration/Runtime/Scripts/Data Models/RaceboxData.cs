using System;

namespace RaceboxIntegration.DataModels
{
    /// <summary>
    /// Data model representing the parsed information from a Racebox device.
    /// Contains all sensor data including GPS coordinates, IMU readings, and device status.
    /// </summary>
    [Serializable]
    public class RaceboxData
    {
        // Raw parsed fields from the packet
        public uint iTOW;
        public ushort year;
        public byte month;
        public byte day;
        public byte hour;
        public byte minute;
        public byte second;
        public byte validityFlags;
        public uint timeAccuracy;
        public int nanoseconds;
        public byte fixStatus;
        public byte fixStatusFlags;
        public byte dateTimeFlags;
        public byte numSVs;
        public int longitude;
        public int latitude;
        public int wgsAltitude;
        public int mslAltitude;
        public uint horizontalAccuracy;
        public uint verticalAccuracy;
        public int speed;
        public int heading;
        public uint speedAccuracy;
        public uint headingAccuracy;
        public ushort pdop;
        public byte latLonFlags;
        public byte batteryStatus;
        public short gForceX;
        public short gForceY;
        public short gForceZ;
        public short rotRateX;
        public short rotRateY;
        public short rotRateZ;

        // Derived fields computed from flags and data
        public bool validDate;
        public bool validTime;
        public bool fullyResolved;
        public bool validMagDecl;
        public bool validFix;
        public bool diffCorrApplied;
        public int powerState;
        public bool validHeading;
        public int carrierPhase;
        public bool dateTimeConfirmed;
        public bool dateValidConfirmed;
        public bool timeValidConfirmed;
        public bool invalidLatLon;
        public int diffCorrAge;
        public bool isCharging;
        public int batteryPercent;

        // Derived timestamp string
        public string timestamp;
    }
}