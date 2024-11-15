using LibDP100;
using Spectre.Console;
using System;
using System.Threading;

namespace PowerSupplyApp
{
    partial class Program
    {
        private const int numDataRows = 19;
        private const int numUserEntryRows = 4; //6; OPP/OTP/RPP/Auto_OUT/Backlight/Volume are not supported for "set" operations due underlying driver sending the wrong data length.
        private const int numUserEntryCols = 5; // Five digits. Perhaps this should be specific to each row.
        private const ushort largeIncrement = 10;

        private static bool runInteractive;
        private static bool showControls = false;
        private static bool showDeviceInfo = false;

        // Handles the layout of the user interface.
        // Used to easily switch between different screens/views.
        private static Layout layout;

        // The selected row which directly relates to the parameter selected (from top to bottom).
        private static int selectedRow = 0;

        // The character column. Unlike typical terminal column order,
        // this is from right to left and is meant to reflect the digit in a parameter that is selected.
        private static int selectedCol = 0;

        static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            runInteractive = false;
            e.Cancel = true;
        }

        public static void RunInteractiveMode()
        {
            runInteractive = true;
            EnterAlternateScreenBuffer();
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Title = psu.Device.Type;

            psu.RefreshPreset(psu.Output.Preset);
            psu.RefreshSystemParams();
            sys = new PowerSupplySystemParams(psu.SystemParams);
            layout = new Layout("Root");

            psu.ActualOutputEvent += ReceiveActualOutput;
            psu.StartWorkerThread(TimeSpan.FromMilliseconds(1));

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
            Thread.Sleep(500);

            ExitAlternateScreenBuffer();
        }

        public static void RunWaveGenMode()
        {
            while (runInteractive)
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
            while (runInteractive)
            {
                ProcessKeys(psu);
                Thread.Sleep(10);
            }
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
                psu.SetSetpoint(sp);
            }
            else
            {
                psu.SetSystemParams(sys);
            }
        }

        private static void ProcessKeys(PowerSupply psu)
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
                    break;
                case KeyboardEvent.SetPreset1:
                case KeyboardEvent.SetPreset2:
                case KeyboardEvent.SetPreset3:
                case KeyboardEvent.SetPreset4:
                case KeyboardEvent.SetPreset5:
                case KeyboardEvent.SetPreset6:
                case KeyboardEvent.SetPreset7:
                case KeyboardEvent.SetPreset8:
                case KeyboardEvent.SetPreset9:
                    psu.SetOutputToPreset((byte)(keyEvent - KeyboardEvent.SetPreset0));
                    sp = new PowerSupplySetpoint(psu.Output.Setpoint);
                    break;
                case KeyboardEvent.SavePreset0:
                case KeyboardEvent.SavePreset1:
                case KeyboardEvent.SavePreset2:
                case KeyboardEvent.SavePreset3:
                case KeyboardEvent.SavePreset4:
                case KeyboardEvent.SavePreset5:
                case KeyboardEvent.SavePreset6:
                case KeyboardEvent.SavePreset7:
                case KeyboardEvent.SavePreset8:
                case KeyboardEvent.SavePreset9:
                    //psu.SavePreset((byte)(keyEvent - KeyboardEvent.SavePreset0), sp);
                    psu.SetSetpointPreset((byte)(keyEvent - KeyboardEvent.SavePreset0), sp);
                    break;

                case KeyboardEvent.IncrementVoltage:
                    VoltageOutput++;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.DecrementVoltage:
                    VoltageOutput--;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.IncrementVoltage10x:
                    VoltageOutput += largeIncrement;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.DecrementVoltage10x:
                    VoltageOutput -= largeIncrement;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.IncrementCurrent:
                    CurrentOutput++;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.DecrementCurrent:
                    CurrentOutput--;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.IncrementCurrent10x:
                    CurrentOutput += largeIncrement;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.DecrementCurrent10x:
                    CurrentOutput -= largeIncrement;
                    psu.SetSetpoint(sp);
                    break;
                case KeyboardEvent.OutputOn:
                    psu.SetOutputOn();
                    break;
                case KeyboardEvent.OutputOff:
                    psu.SetOutputOff();
                    break;
                case KeyboardEvent.OutputToggle:
                    if (psu.Output.On)
                        psu.SetOutputOff();
                    else
                        psu.SetOutputOn();
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
        }

        static void ReceiveActualOutput(PowerSupplyActuals output)
        {
            if (showControls)
            {
                layout["Root"].Update(GetControlsPanel());
            }
            else if (showDeviceInfo)
            {
                layout["Root"].Update(GetDeviceInfoPanel());
            }
            else
            {
                layout["Root"].Update(GetDataGrid(psu, sp, sys, output));
            }

            Console.SetCursorPosition(0, 0);
            AnsiConsole.Write(layout);
        }
    }
}
