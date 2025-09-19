using System;
using System.Runtime.InteropServices;

namespace PMD2
{
    public static class Constants
    {
        // 原 Python constants
        public const string PRODUCT_NAME = "PMD2";
        public const string PRODUCT_STRING = "ElmorLabs PMD2";
        //public const byte VENDOR_ID = 0xEE;
        //public const byte PRODUCT_ID = 0x15;
        //public const byte FIRMWARE_VERSION = 0x00;
        public const byte VENDOR_ID = 0xEE;
        public const byte PRODUCT_ID = 0x15;
        public const byte FIRMWARE_VERSION = 0x00;

        public const int SENSOR_POWER_NUM = 10;
        public const int NUM_SAMPLES = 500;
    }

    public enum UART_CMD : byte
    {
        CMD_WELCOME = 0,
        CMD_READ_VENDOR_DATA = 1,
        CMD_READ_UID = 2,
        CMD_READ_DEVICE_DATA = 3,
        CMD_READ_SENSOR_VALUES = 4,
        CMD_WRITE_CONT_TX = 5,
        CMD_READ_CALIBRATION = 6,
        CMD_WRITE_CALIBRATION = 7,
        CMD_LOAD_CALIBRATION = 8,
        CMD_STORE_CALIBRATION = 9,
        CMD_RESET = 0xF0,
        CMD_BOOTLOADER = 0xF1,
        CMD_NVM_CONFIG = 0xF2,
        CMD_NOP = 0xFF
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VendorDataStruct
    {
        public byte VendorId;
        public byte ProductId;
        public byte FwVersion;

        public override string ToString()
        {
            return $"VendorId: {VendorId:X}, ProductId: {ProductId:X}, FwVersion: {FwVersion:X}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PowerSensor
    {
        public short Voltage;   // 2 bytes
        public int Current;     // 4 bytes
        public int Power;       // 4 bytes

        public override string ToString()
        {
            return $"Voltage: {Voltage}, Current: {Current}, Power: {Power}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SensorStruct
    {
        public ushort Vdd;
        public short Tchip;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SENSOR_POWER_NUM)]
        public PowerSensor[] PowerReadings;

        public ushort EpsPower;
        public ushort PciePower;
        public ushort MbPower;
        public ushort TotalPower;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SENSOR_POWER_NUM)]
        public byte[] Ocp;
    }
}
