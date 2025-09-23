using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace PMD2
{
    public class SerialComm
    {
        private SerialPort serialPort;

        public bool IsOpen => (serialPort != null && serialPort.IsOpen);

        public bool OpenPort()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                if (ports.Length == 0)
                {
                    Console.WriteLine("No available serial ports found.");
                    return false;
                }
                return Open(ports[0], 1500000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open serial port: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 1) 驗證「ElmorLabs PMD2」歡迎字串
        /// 2) 檢查 VendorDataStruct 是否符合
        /// </summary>
        public bool DeviceHandshake()
        {
            if (!IsOpen)
            {
                Console.WriteLine("Serial port not open.");
                return false;
            }

            int attempt = 0;
            int maxRetries = 3; // 可以嘗試最多幾次
            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    // 嘗試一次讀取
                    byte[] buf = new byte[Constants.PRODUCT_STRING.Length + 1];
                    int len = serialPort.Read(buf, 0, buf.Length);

                    if (len < buf.Length)
                        throw new Exception("Read welcome string failed (not enough bytes).");

                    string readStr = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                    if (readStr != (Constants.PRODUCT_STRING + "\0"))
                        throw new Exception($"Welcome string mismatch! Expect: \"{Constants.PRODUCT_STRING}\\0\", Got: \"{readStr}\"");

                    Console.WriteLine($"Welcome string OK: {readStr}");

                    // 後續繼續讀 VendorDataStruct ...
                    // (若成功就 return true)

                    return true;
                }
                catch (TimeoutException tex)
                {
                    Console.WriteLine($"Attempt {attempt}: Timeout => {tex.Message}");
                    if (attempt < maxRetries)
                    {
                        // 等個 100~200 ms，再重試
                        System.Threading.Thread.Sleep(200);
                    }
                    else
                    {
                        Console.WriteLine("Handshake failed due to repeated Timeout.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // 其它例外，直接失敗
                    Console.WriteLine($"Handshake failed: {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 3) 取得裝置的 UID，並組合成 Guid
        /// </summary>
        public Guid? GetUID()
        {
            if (!IsOpen)
            {
                Console.WriteLine("Port not open, cannot read UID.");
                return null;
            }

            try
            {
                // 發送 CMD_READ_UID 要求 12 bytes UID (假設 CMD_READ_UID = 2)
                // 若實際 enum 不是 2，請修正對應值
                serialPort.Write(new byte[] { (byte)UART_CMD.CMD_READ_UID }, 0, 1);

                // 讀取 12 bytes
                byte[] rxBuf = new byte[12];
                int readLen = serialPort.Read(rxBuf, 0, rxBuf.Length);
                if (readLen < rxBuf.Length)
                {
                    throw new Exception("UID data read failed (not enough bytes).");
                }

                // 官方範例做法：將 12 bytes 反轉後放到 guid_buffer[4..15]
                // 這意味著前4 bytes 保留 0，用來生成一個 16 bytes 的 GUID
                byte[] guidBuffer = new byte[16]; // 預設全是 0
                for (int i = 0; i < 12; i++)
                {
                    guidBuffer[i + 4] = rxBuf[11 - i];
                }
                // 產生 C# Guid
                Guid deviceGuid = new Guid(guidBuffer);
                Console.WriteLine($"Device UID = {deviceGuid}");

                return deviceGuid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUID failed: {ex.Message}");
                return null;
            }
        }

        // ----------------------------------------------------------------------
        // 讀取感測值結構
        // ----------------------------------------------------------------------
        public SensorStruct? ReadSensorStruct()
        {
            if (!IsOpen) return null;

            serialPort.Write(new byte[] { (byte)UART_CMD.CMD_READ_SENSOR_VALUES }, 0, 1);

            int structSize = 2 + 2
                             + (Constants.SENSOR_POWER_NUM * 10)
                             + 2 + 2 + 2 + 2
                             + Constants.SENSOR_POWER_NUM;

            byte[] buffer = new byte[structSize];
            int readLen = serialPort.Read(buffer, 0, structSize);
            if (readLen != structSize)
            {
                return null;
            }

            return ParseSensorStruct(buffer);
        }

        private SensorStruct ParseSensorStruct(byte[] data)
        {
            SensorStruct s = new SensorStruct();
            s.PowerReadings = new PowerSensor[Constants.SENSOR_POWER_NUM];
            s.Ocp = new byte[Constants.SENSOR_POWER_NUM];

            int offset = 0;
            s.Vdd = BitConverter.ToUInt16(data, offset); offset += 2;
            s.Tchip = BitConverter.ToInt16(data, offset); offset += 2;

            for (int i = 0; i < Constants.SENSOR_POWER_NUM; i++)
            {
                short vol = BitConverter.ToInt16(data, offset);
                int cur = BitConverter.ToInt32(data, offset + 2);
                int pow = BitConverter.ToInt32(data, offset + 6);
                offset += 10;

                s.PowerReadings[i] = new PowerSensor
                {
                    Voltage = vol,
                    Current = cur,
                    Power = pow
                };
            }

            s.EpsPower = BitConverter.ToUInt16(data, offset); offset += 2;
            s.PciePower = BitConverter.ToUInt16(data, offset); offset += 2;
            s.MbPower = BitConverter.ToUInt16(data, offset); offset += 2;
            s.TotalPower = BitConverter.ToUInt16(data, offset); offset += 2;

            for (int i = 0; i < Constants.SENSOR_POWER_NUM; i++)
            {
                s.Ocp[i] = data[offset + i];
            }
            offset += Constants.SENSOR_POWER_NUM;

            return s;
        }

        public bool Open(string portName, int baudRate)
        {
            try
            {
                // 若先前尚未釋放，先關閉
                Close();

                serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = false,
                    RtsEnable = false,
                    NewLine = "\n"
                };

                serialPort.Open();

                // 你原本做的 RTS「落下→等待→拉高」序列，保留
                serialPort.RtsEnable = false;
                System.Threading.Thread.Sleep(200);
                serialPort.RtsEnable = true;
                System.Threading.Thread.Sleep(200);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open {portName} @ {baudRate}: {ex.Message}");
                // 若開啟失敗也確保釋放
                Close();
                return false;
            }
        }

        public void Close()
        {
            try
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                    {
                        // 如有背景讀取執行緒，務必先停止（上層已做 reading=false/cancel）
                        serialPort.Close();
                    }
                    serialPort.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close port error: {ex.Message}");
            }
            finally
            {
                serialPort = null; // 很重要：清成 null 以便之後能乾淨重連
            }
        }

    }
}
