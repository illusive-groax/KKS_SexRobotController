using KKS_SexRobotController.Helpers;
using KKS_SexRobotController.Plugin;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace KKS_SexRobotController.RobotController
{
    internal sealed class SerialPortConnection
    {
        internal SerialPort SRC_SerialPort { get; set; }

        private static SerialPortConnection _instance;
        private static readonly object _lock = new();

        private SerialPortConnection()
        {
            SRC_SerialPort = new SerialPort();
        }

        internal static SerialPortConnection GetInstance()
        {
            // prevent threads stumbling over the lock once the instance is ready.
            if (_instance == null)
            {
                // if just launched, lock the instance
                lock (_lock)
                {
                    // only create a new instance, if one doesn't already exist
                    if (_instance == null)
                    {
                        _instance = new SerialPortConnection();
                    }
                }
            }
            return _instance;
        }

        internal void UpdateSerialPort()
        {

            KKS_SexRobotControllerPlugin.SerialPortConnected.Value = false;
            KKS_SexRobotControllerPlugin.LogInfo("Serial COM port changed to: " + KKS_SexRobotControllerPlugin.SerialPortConfig.Value + ".");

            if (SRC_SerialPort != null)
            {
                if (SRC_SerialPort.IsOpen)
                {
                    try
                    {
                        // Close the serial port connection
                        SRC_SerialPort.Close();

                        KKS_SexRobotControllerPlugin.LogInfo("Serial port " + SRC_SerialPort.PortName + " has been disconnected.");
                    }
                    catch (Exception e)
                    {
                        KKS_SexRobotControllerPlugin.LogDebug("Error: " + e.ToString() + ".");
                    }
                }
            }

            KKS_SexRobotControllerPlugin.SerialPortStatus.Value = KKS_SexRobotControllerPlugin.SerialPortConfig.Value + " port is disconnected.";
        }

        internal void UpdateSerialPortConnection()
        {

            // Disconnect serial port if currently connected
            if (SRC_SerialPort != null)
            {
                if (SRC_SerialPort.IsOpen)
                {
                    try
                    {
                        // Close the serial port connection
                        SRC_SerialPort.Close();

                        KKS_SexRobotControllerPlugin.SerialPortStatus.Value = SRC_SerialPort.PortName + " port is disconnected.";

                        KKS_SexRobotControllerPlugin.LogInfo("Serial port " + SRC_SerialPort.PortName + " has been disconnected.");
                    }
                    catch (Exception e)
                    {
                        KKS_SexRobotControllerPlugin.SerialPortStatus.Value = SRC_SerialPort.PortName + " port is disconnected.";

                        KKS_SexRobotControllerPlugin.LogInfo("Serial port " + SRC_SerialPort.PortName + " has been disconnected.");

                        KKS_SexRobotControllerPlugin.LogDebug("Error: " + e.ToString() + ".");
                    }
                }
            }

            // Connect to serial port
            if (KKS_SexRobotControllerPlugin.SerialPortConnected.Value)
            {
                // Setup COM port based on updated config selection
                SRC_SerialPort = new SerialPort("\\\\.\\" + KKS_SexRobotControllerPlugin.SerialPortConfig.Value, 115200);

                try
                {
                    // Open the serial port connection
                    SRC_SerialPort.Open();

                    if (SRC_SerialPort.IsOpen)
                    {
                        KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Connected to serial port " + KKS_SexRobotControllerPlugin.SerialPortConfig.Value + ".";

                        KKS_SexRobotControllerPlugin.SerialPortConnected.Value = true;

                        KKS_SexRobotControllerPlugin.LogInfo("Connected to serial port " + SRC_SerialPort.PortName + ".");
                    }
                    else
                    {
                        KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Error connecting to serial port " + KKS_SexRobotControllerPlugin.SerialPortConfig.Value + ".";

                        KKS_SexRobotControllerPlugin.SerialPortConnected.Value = false;

                        KKS_SexRobotControllerPlugin.LogDebug("Error connecting to serial port " + SRC_SerialPort.PortName + ".");
                    }
                }
                catch (Exception e)
                {
                    KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Error connecting to serial port " + KKS_SexRobotControllerPlugin.SerialPortConfig.Value + ".";

                    KKS_SexRobotControllerPlugin.SerialPortConnected.Value = false;

                    KKS_SexRobotControllerPlugin.LogDebug("Error: " + e.ToString() + ".");
                }
            }
        }

        private async Task UpdateConnectRobotButton()
        {
            await Task.Run(async () =>
            {
                if (SRC_SerialPort.IsOpen)
                {
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonConnectRobot_Connected;
                }
                else
                {
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonConnectRobot_NotConnected;
                }
                await Task.Delay(1000);
                KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonConnectRobot_Text;
                KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonDisconnectRobot_Text;
            });
        }

        private async Task UpdateDisconnectRobotButton()
        {
            await Task.Run(async () =>
            {
                if (!SRC_SerialPort.IsOpen)
                {
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonDisconnectRobot_Disconnected;
                }
                else
                {
                    KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonDisconnectRobot_NotDisconnected;
                }
                await Task.Delay(1000);
                KKS_SexRobotControllerPlugin.BtnConnectRobotText.text = StringConstants.ButtonConnectRobot_Text;
            });
        }

        internal void CheckButtonAndSerialConnState()
        {
            // Check if connect robot button was clicked
            if (KKS_SexRobotControllerPlugin.BtnConnectRobotClicked)
            {
                KKS_SexRobotControllerPlugin.BtnConnectRobotClicked = false;

                if (KKS_SexRobotControllerPlugin.SerialPortConnected.Value)
                {
                    UpdateSerialPortConnection();
                }
                else
                {
                    KKS_SexRobotControllerPlugin.SerialPortConnected.Value = true;
                }

                _ = UpdateConnectRobotButton();
            }

            // Check if connect robot button was clicked
            if (KKS_SexRobotControllerPlugin.BtnDisconnectRobotClicked)
            {
                KKS_SexRobotControllerPlugin.BtnDisconnectRobotClicked = false;

                if (!KKS_SexRobotControllerPlugin.SerialPortConnected.Value)
                {
                    UpdateSerialPortConnection();
                }
                else
                {
                    KKS_SexRobotControllerPlugin.SerialPortConnected.Value = false;
                }

                _ = UpdateDisconnectRobotButton();
            }

            // Check if serial port connection toggle hotkey was pressed and toggle the serial port on/off if so
            if (KKS_SexRobotControllerPlugin.ToggleSerialPortConnection.Value.IsDown())
            {
                bool connectSerialPort = false;

                if (SRC_SerialPort != null)
                {
                    if (SRC_SerialPort.IsOpen)
                    {
                        try
                        {
                            KKS_SexRobotControllerPlugin.SerialPortConnected.Value = false;

                            KKS_SexRobotControllerPlugin.SerialPortStatus.Value = SRC_SerialPort.PortName + " port is disconnected.";

                            // Close the serial port connection
                            SRC_SerialPort.Close();

                            Task task = UpdateDisconnectRobotButton();

                            KKS_SexRobotControllerPlugin.LogInfo("Serial port " + SRC_SerialPort.PortName + " has been disconnected.");
                        }
                        catch (Exception e)
                        {
                            KKS_SexRobotControllerPlugin.SerialPortStatus.Value = SRC_SerialPort.PortName + " port is disconnected.";
                            _ = UpdateDisconnectRobotButton();

                            KKS_SexRobotControllerPlugin.LogInfo("Serial port " + SRC_SerialPort.PortName + " has been disconnected.");

                            KKS_SexRobotControllerPlugin.LogDebug("Error: " + e.ToString() + ".");
                        }
                    }
                    else
                    {
                        connectSerialPort = true;
                    }
                }
                else
                {
                    connectSerialPort = true;
                }

                if (connectSerialPort)
                {
                    try
                    {
                        // Setup COM port based on config selection
                        SRC_SerialPort = new SerialPort("\\\\.\\" + KKS_SexRobotControllerPlugin.SerialPortConfig.Value, 115200);

                        // Open the serial port connection
                        SRC_SerialPort.Open();

                        if (SRC_SerialPort.IsOpen)
                        {
                            KKS_SexRobotControllerPlugin.SerialPortConnected.Value = true;

                            KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Connected to serial port " + KKS_SexRobotControllerPlugin.SerialPortConfig.Value + ".";

                            Task task = UpdateConnectRobotButton();

                            KKS_SexRobotControllerPlugin.LogInfo("Connected to serial port " + SRC_SerialPort.PortName + ".");
                        }
                        else
                        {
                            KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Error connecting to serial port " + SRC_SerialPort.PortName + ".";

                            Task task = UpdateConnectRobotButton();

                            KKS_SexRobotControllerPlugin.LogDebug("Error connecting to serial port " + SRC_SerialPort.PortName + ".");
                        }
                    }
                    catch (Exception e)
                    {
                        KKS_SexRobotControllerPlugin.SerialPortStatus.Value = "Error connecting to serial port " + SRC_SerialPort.PortName + ".";
                        _ = UpdateConnectRobotButton();

                        KKS_SexRobotControllerPlugin.LogDebug("Error connecting to serial port " + SRC_SerialPort.PortName + ".");

                        KKS_SexRobotControllerPlugin.LogDebug("Error: " + e.ToString() + ".");
                    }
                }
            }
        }
    }
}
