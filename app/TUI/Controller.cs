using LibDP100;
using PowerSupplyApp.TUI;
using Spectre.Console;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        private const int numDataRows = 19;
        private const int numUserEntryRows = 6;
        private const int numUserEntryCols = 5; // Five digits. Perhaps this should be specific to each row.
        private const ushort largeIncrement = 10;

        private static bool runInteractive;
        private static bool showControls = false;
        private static bool showDeviceInfo = false;

        // Handles the layout of the user interface.
        // Used to easily switch between different screens/views.
        private static Layout layout = new("Root");

        // The selected row which directly relates to the parameter selected (from top to bottom).
        private static int selectedRow = 0;

        // The character column. Unlike typical terminal column order,
        // this is from right to left and is meant to reflect the digit in a parameter that is selected.
        private static int selectedCol = 0;

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            runInteractive = false;
            e.Cancel = true;
        }

        public static void RunInteractiveMode(TimeSpan sleepTime, bool debug)
        {
            if (psu == null)
            {
                return;
            }

            runInteractive = true;
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Title = psu.Device.Type;

            psu.GetPreset(psu.Output.Preset);
            psu.GetSystemParams();
            sys = new PowerSupplySystemParams(psu.SystemParams);

            psu.DebugMode = false;
            psu.ActiveStateEvent += ReceiveActiveState;
            EnterAlternateScreenBuffer();
            psu.StartWorkerThread(sleepTime);

            if (wavegenMode)
            {
                // Deselect a valid row to hid cursor.
                selectedRow = -1;
                RunWaveGenMode();
            }
            else
            {
                RunNormalMode();
            }

            psu.StopWorkerThread();
            ExitAlternateScreenBuffer();
            psu.DebugMode = debug;
        }

        public static void RunWaveGenMode()
        {
            if (psu == null)
            {
                return;
            }

            while (runInteractive && psu.Connected)
            {
                WaveGen.Run();

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    // Flush the input buffer.
                    while (Console.KeyAvailable)
                        Console.ReadKey(false);

                    switch (key.KeyChar)
                    {
                        case 'q':
                        case 'Q':
                            runInteractive = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public static void RunNormalMode()
        {
            if (psu == null)
            {
                return;
            }

            while (runInteractive && psu.Connected)
            {
                if (ProcessKeys(psu))
                {
                    psu.SignalRunWorker();
                }
                Thread.Sleep(10);
            }
        }

        public static void RunBlinker()
        {
            if (psu == null)
            {
                return;
            }

            string blinkMsg;
            int blinkCount = 10;
            runInteractive = true;
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Title = psu.Device.Type;
            blinkMsg = $"Blinking: {psuSerialNumber} ...";

            while (blinkCount-- > 0 && runInteractive && psu.Connected)
            {
                psu.ActiveStateEvent += Blinker;
                Console.WriteLine(blinkMsg);
                psu.StartWorkerThread(TimeSpan.FromMilliseconds(1));
                Thread.Sleep(50);
                psu.StopWorkerThread();
                Thread.Sleep(950);
            }

            Console.WriteLine("Done.");
        }

        private static void ProcessControlValueChange(int increment)
        {
            const int numSetpointControls = 4;

            // Process the new setpoint state with proper thresholding.
            switch (selectedRow)
            {
                case 0:
                    VoltageOutput += increment;
                    break;
                case 1:
                    CurrentOutput += increment;
                    break;
                case 2:
                    OVPOutput += increment;
                    break;
                case 3:
                    OCPOutput += increment;
                    break;
                case 4:
                    OPPOutput += increment;
                    break;
                case 5:
                    OTPOutput += increment;
                    break;
                default:
                    return;
            }

            if (selectedRow <= numSetpointControls - 1)
            {
                psu?.SetOutput(sp);
            }
            else
            {
                psu?.SetSystemParams(sys);
            }
        }

        private static bool ProcessKeys(PowerSupply supply)
        {
            KeyboardEvent keyEvent = GetKeyboardEvent();

            switch (keyEvent)
            {
                case KeyboardEvent.IncrementDigit:
                    ProcessControlValueChange((int)Math.Pow(10, selectedCol));
                    break;
                case KeyboardEvent.DecrementDigit:
                    ProcessControlValueChange((int)-Math.Pow(10, selectedCol));
                    break;
                case KeyboardEvent.PreviousControl:
                    if (selectedRow > 0)
                    {
                        selectedRow--;
                    }
                    break;
                case KeyboardEvent.NextControl:
                    if (selectedRow < numUserEntryRows - 1)
                    {
                        selectedRow++;
                    }
                    break;
                case KeyboardEvent.PreviousDigit:
                    if (selectedCol > 0)
                    {
                        selectedCol--;
                    }
                    break;
                case KeyboardEvent.NextDigit:
                    if (selectedCol < numUserEntryCols - 1)
                    {
                        selectedCol++;
                    }
                    break;

                case KeyboardEvent.ReturnToNormal:
                    ViewMode = ViewMode.Normal;
                    showControls = false;
                    showDeviceInfo = false;
                    break;
                case KeyboardEvent.Quit:
                    runInteractive = false;
                    break;
                case KeyboardEvent.SetPreset0:
                case KeyboardEvent.SetPreset1:
                case KeyboardEvent.SetPreset2:
                case KeyboardEvent.SetPreset3:
                case KeyboardEvent.SetPreset4:
                case KeyboardEvent.SetPreset5:
                case KeyboardEvent.SetPreset6:
                case KeyboardEvent.SetPreset7:
                case KeyboardEvent.SetPreset8:
                case KeyboardEvent.SetPreset9:
                    supply.UsePreset((byte)(keyEvent - KeyboardEvent.SetPreset0), fromNonVolatile: true);
                    sp = new PowerSupplySetpoint(supply.Output.Setpoint);
                    break;
                case KeyboardEvent.RecallPreset0:
                case KeyboardEvent.RecallPreset1:
                case KeyboardEvent.RecallPreset2:
                case KeyboardEvent.RecallPreset3:
                case KeyboardEvent.RecallPreset4:
                case KeyboardEvent.RecallPreset5:
                case KeyboardEvent.RecallPreset6:
                case KeyboardEvent.RecallPreset7:
                case KeyboardEvent.RecallPreset8:
                case KeyboardEvent.RecallPreset9:
                    supply.UsePreset((byte)(keyEvent - KeyboardEvent.RecallPreset0), fromNonVolatile: false);
                    sp = new PowerSupplySetpoint(supply.Output.Setpoint);
                    break;
                case KeyboardEvent.SavePreset0: // Saving to Preset 0 does not appear to work.
                case KeyboardEvent.SavePreset1:
                case KeyboardEvent.SavePreset2:
                case KeyboardEvent.SavePreset3:
                case KeyboardEvent.SavePreset4:
                case KeyboardEvent.SavePreset5:
                case KeyboardEvent.SavePreset6:
                case KeyboardEvent.SavePreset7:
                case KeyboardEvent.SavePreset8:
                case KeyboardEvent.SavePreset9:
                    supply.SetPreset((byte)(keyEvent - KeyboardEvent.SavePreset0), sp);
                    break;

                case KeyboardEvent.IncrementVoltage:
                    VoltageOutput++;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.DecrementVoltage:
                    VoltageOutput--;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.IncrementVoltage10x:
                    VoltageOutput += largeIncrement;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.DecrementVoltage10x:
                    VoltageOutput -= largeIncrement;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.IncrementCurrent:
                    CurrentOutput++;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.DecrementCurrent:
                    CurrentOutput--;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.IncrementCurrent10x:
                    CurrentOutput += largeIncrement;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.DecrementCurrent10x:
                    CurrentOutput -= largeIncrement;
                    supply.SetOutput(sp);
                    break;
                case KeyboardEvent.OutputOn:
                    supply.SetOutputOn();
                    break;
                case KeyboardEvent.OutputOff:
                    supply.SetOutputOff();
                    break;
                case KeyboardEvent.OutputToggle:
                    if (supply.Output.On)
                        supply.SetOutputOff();
                    else
                        supply.SetOutputOn();
                    break;
                case KeyboardEvent.ShowControls:
                    showControls = true;
                    ViewMode = ViewMode.Controls;
                    break;
                case KeyboardEvent.ShowDeviceInfo:
                    showDeviceInfo = true;
                    ViewMode = ViewMode.DeviceInfo;
                    break;

                case KeyboardEvent.None:
                default:
                    break;
            }

            return keyEvent != KeyboardEvent.None;
        }

        private static void ReceiveActiveState(PowerSupplyActiveState activeState)
        {
            if (showControls)
            {
                layout["Root"].Update(GetControlsPanel());
            }
            else if (psu != null)
            {
                if (showDeviceInfo)
                {
                    layout["Root"].Update(GetDeviceInfoPanel(psu));
                }
                else
                {
                    layout["Root"].Update(GetDataGrid(psu, sp, sys, activeState));
                }
            }

            Console.SetCursorPosition(0, 0);
            AnsiConsole.Write(layout);
        }

        private static void Blinker(PowerSupplyActiveState activeState)
        {
            // Nothing to do.
        }
    }
}
