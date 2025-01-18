using LibDP100;
using System;
using Spectre.Console;
using System.Collections.Generic;

namespace PowerSupplyApp
{
    partial class Program
    {
        private static ColorScheme normalGreenScheme = new ColorScheme
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkGreen, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.DarkGreen),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            NumericDataChanged = new Style(Color.White, null, Decoration.Invert),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.DarkGreen),
            FaultMessage = new Style(Color.White, Color.DarkRed),
        };

        private static ColorScheme normalRedScheme = new ColorScheme
        {
            TableAccent = Color.Grey,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, Color.Grey50),
            Preset = new Style(Color.Black, Color.Grey),
            PresetSelected = new Style(Color.White, Color.DarkRed),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            NumericDataChanged = new Style(Color.White, null, Decoration.Invert),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            FaultMessage = new Style(Color.White, Color.DarkRed),
        };

        private static ColorScheme lockSchemeGold = new ColorScheme
        {
            TableAccent = Color.Gold1,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            NumericDataChanged = new Style(Color.White, null, Decoration.Invert),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            FaultMessage = new Style(Color.White, Color.DarkRed),
        };

        private static ColorScheme faultedSchemeRed = new ColorScheme
        {
            TableAccent = Color.DarkRed,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, Color.Grey50),
            Preset = new Style(Color.Black, Color.Grey),
            PresetSelected = new Style(Color.White, Color.DarkRed),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            NumericDataChanged = new Style(Color.White, null, Decoration.Invert),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            FaultMessage = new Style(Color.White, Color.DarkRed),
        };

        // The schemes selected for the different operational states.
        private static ColorScheme normalScheme = normalRedScheme;
        private static ColorScheme lockScheme = lockSchemeGold;
        private static ColorScheme faultScheme = faultedSchemeRed;

        // The scheme that is actively being applied to the TUI.
        private static ColorScheme scheme = normalScheme;

        private static ViewMode ViewMode { get; set; } = ViewMode.Normal;

        private static bool Faulted
        {
            get { return faulted; }
            set
            {
                faulted = value;
                if (faulted)
                {
                    scheme = faultScheme;
                }
                else if (ControlsLocked)
                {
                    scheme = lockScheme;
                }
                else
                {
                    scheme = normalScheme;
                }
            }
        }

        private static bool ControlsLocked
        {
            get { return controlsLocked; }
            set
            {
                controlsLocked = value;
                if (faulted)
                {
                    scheme = faultScheme;
                }
                else if (ControlsLocked)
                {
                    scheme = lockScheme;
                }
                else
                {
                    scheme = normalScheme;
                }
            }
        }

        private static bool faulted = false;
        private static bool controlsLocked = false;

        /// <summary>
        /// Write the ANSI sequence to enter the alternate screen buffer.
        /// </summary>
        private static void EnterAlternateScreenBuffer()
        {
            // Enter alt-screen buffer and hide cursor.
            Console.Write($"{(char)27}[?1049h");
            Console.Write($"{(char)27}[?25l");
        }

        /// <summary>
        /// Write the ANSI sequence to exit the alternate screen buffer.
        /// </summary>
        private static void ExitAlternateScreenBuffer()
        {
            // Exit alt-screen buffer and show cursor.
            Console.Write($"{(char)27}[?1049l");
            Console.Write($"{(char)27}[?25h");
        }

        /// <summary>
        /// Convert the input value to it visual representation where leading zeros are dim
        /// and if the value is currently the selected row, highlight the current digit that
        /// is to be controlled.
        /// </summary>
        /// <param name="value">The current value to convert.</param>
        /// <param name="selected">Indicates that the value is selected by the user.</param>
        /// <param name="selectedDigit">Indicates which digit is currently being controlled.</param>
        /// <returns>The converted markup text</returns>
        private static string GetUserEntryString(ushort value, bool selected = false, int selectedDigit = 0)
        {
            const int totalDigits = 5;
            string rawTextDigits = $"{value}";
            string markupText = "";

            if (totalDigits - rawTextDigits.Length > 0)
            {
                markupText = "[dim]";

                for (int i = totalDigits - 1; i >= rawTextDigits.Length; i--)
                {
                    if (selected && (i == selectedDigit))
                    {
                        markupText += $"[underline]0[/]";
                    }
                    else
                    {
                        markupText += "0";
                    }
                }
                markupText += "[/]";
            }

            if (selected)
            {

                for (int i = 0; i < rawTextDigits.Length; i++)
                {
                    if (i == ((rawTextDigits.Length - 1) - selectedDigit))
                    {
                        markupText += $"[underline]{rawTextDigits[i]}[/]";
                    }
                    else
                    {
                        markupText += rawTextDigits[i];
                    }

                }

            }
            else
            {
                markupText += rawTextDigits;
            }

            return markupText;
        }

        /// <summary>
        /// Gets the Grid for the device information.
        /// </summary>
        /// <returns>The Grid containing the device information.</returns>
        private static Grid GetDeviceInfoGrid()
        {
            return new Grid()
                .AddColumns(2)
                .AddRow(new Markup("[blue]Device[/]"), new Markup(psu.Device.Type))
                .AddRow(new Markup("[blue]Serial Num[/]"), new Markup(psu.Device.SerialNumber))
                .AddRow(new Markup("[blue]HW Version[/]"), new Markup(psu.Device.HardwareVersion))
                .AddRow(new Markup("[blue]SW Version[/]"), new Markup(psu.Device.SoftwareVersion))
                .AddRow(new Markup("[blue]SW State[/]"), new Markup(psu.Device.SoftwareState));
        }

        /// <summary>
        /// Gets the Grid for the user controls.
        /// </summary>
        /// <returns>The Grid containing the user controls.</returns>
        private static Grid GetControlsGrid()
        {
            return new Grid()
                .AddColumn(new GridColumn().RightAligned())
                .AddColumn()
                .AddRow(new Markup("[blue]`[/]/[blue]~[/]"), new Markup("Output TOGGLE"))
                .AddRow(new Markup("[blue]=[/]/[blue]+[/]"), new Markup("Output ON"))
                .AddRow(new Markup("[blue]-[/]/[blue]_[/]"), new Markup("Output OFF (Fault Reset)"))
                .AddRow(new Markup("[blue]w[/]/[blue]W[/]"), new Markup("Increment Millivolts (x1/x10)"))
                .AddRow(new Markup("[blue]v[/]/[blue]V[/]"), new Markup("Decrement Millivolts (x1/x10)"))
                .AddRow(new Markup("[blue]d[/]/[blue]D[/]"), new Markup("Increment Milliamps (x1/x10)"))
                .AddRow(new Markup("[blue]a[/]/[blue]A[/]"), new Markup("Decrement Milliamps (x1/x10)"))
                .AddRow(new Markup("[blue]1[/]-[blue]9[/]"), new Markup("Preset Selection"))
                .AddRow(new Markup("[blue]Alt[/] + [blue]1[/]-[blue]9[/]"), new Markup("Alter/Store Current Setpoints to Preset"))
                .AddRow(new Markup("[blue]↑[/]/[blue]↓[/]/[blue]←[/]/[blue]→[/]"), new Markup("Entry Navigation"))
                .AddRow(new Markup("[blue]Shift[/] + [blue]↑[/]/[blue]↓[/]"), new Markup("Digit Modification"))
                .AddRow(new Markup("[blue]Control[/] + [blue]Shift[/] + [blue]L[/]"), new Markup("Lock/Unlock Device Controls"))
                .AddRow(new Markup("[blue]i[/]/[blue]I[/]"), new Markup("Device Information"))
                .AddRow(new Markup("[blue]q[/]/[blue]Q[/]"), new Markup("Quit Application"))
                .AddRow(new Markup("[blue]/[/]/[blue]?[/]"), new Markup("Show Controls"));
        }

        /// <summary>
        /// Gets the Grid for the protections.
        /// </summary>
        /// <returns>The Grid containing the protections.</returns>
        private static Grid GetProtectionsGrid()
        {
            int rowIndex = 0;
            return new Grid()
                .AddColumns(2)
                .AddRow(
                    new Markup("[white]OVP (mV)[/]", normalScheme.RowHeader),
                    new Markup(GetUserEntryString(sp.OVP, (selectedRow == rowIndex++), selectedCol), normalScheme.NumericData))
                .AddRow(
                    new Markup("[white]OCP (mA)[/]", normalScheme.RowHeader),
                    new Markup(GetUserEntryString(sp.OCP, (selectedRow == rowIndex++), selectedCol), normalScheme.NumericData))
                .AddRow(
                    new Markup("[white]OPP (dW)[/]", normalScheme.RowHeader),
                    new Markup(GetUserEntryString(sys.OPP, (selectedRow == rowIndex++), selectedCol), normalScheme.NumericData))
                .AddRow(
                    new Markup("[white]OTP (C)[/]", normalScheme.RowHeader),
                    new Markup(GetUserEntryString(sys.OTP, (selectedRow == rowIndex++), selectedCol), normalScheme.NumericData));
        }

        /// <summary>
        /// Generate bar charts for indicating the percent load for voltage and current.
        /// This offers at-a-glance feedback to the user how close the device is to the output
        /// limits which may either result in regulation mode changes, or tripping fault
        /// protection logic.
        /// </summary>
        /// <param name="activeState">The current state information of the device.</param>
        /// <returns>The grid.</returns>
        private static Grid GetBarChartGrid(PowerSupplyActiveState activeState)
        {
            int vo_limit = (activeState.VoltageOutputMax > psu.PresetParams[psu.Output.Preset].OVP) ?
                psu.PresetParams[psu.Output.Preset].OVP : activeState.VoltageOutputMax;
            int io_limit = psu.PresetParams[psu.Output.Preset].OCP;

            var width = Console.WindowWidth - 3;
            double vLoad = (double)activeState.Voltage / vo_limit;
            double iLoad = (double)activeState.Current / io_limit;

            return new Grid()
                .AddColumns(2)
                .Centered()
                .AddRow(new Markup("V", scheme.RowHeader), ProgressBar.GetMarkup(vLoad, width, scheme.Bar))
                .AddRow(new Markup("I", scheme.RowHeader), ProgressBar.GetMarkup(iLoad, width, scheme.Bar));
        }

        /// <summary>
        /// Gets the grid object for displaying the preset selection.
        /// </summary>
        /// <returns>The grid.</returns>
        private static Grid GetPresetGrid()
        {
            Markup[] presetText =
            {
                new Markup(" 1 ", scheme.Preset),
                new Markup(" 2 ", scheme.Preset),
                new Markup(" 3 ", scheme.Preset),
                new Markup(" 4 ", scheme.Preset),
                new Markup(" 5 ", scheme.Preset),
                new Markup(" 6 ", scheme.Preset),
                new Markup(" 7 ", scheme.Preset),
                new Markup(" 8 ", scheme.Preset),
                new Markup(" 9 ", scheme.Preset)
            };

            if (psu.Output.Preset > 0)
            {
                presetText[psu.Output.Preset - 1] = new Markup($" {psu.Output.Preset} ", scheme.PresetSelected);
            }

            return new Grid()
                .AddColumns(9)
                .AddRow(presetText[0],
                        presetText[1],
                        presetText[2],
                        presetText[3],
                        presetText[4],
                        presetText[5],
                        presetText[6],
                        presetText[7],
                        presetText[8]);
        }

        private static Markup GetOutputModeMarkup(PowerSupplyOutputMode mode)
        {
            Markup markup;
            string text = Enum.GetName(typeof(PowerSupplyOutputMode), mode);

            switch (mode)
            {
                case PowerSupplyOutputMode.OFF:
                    markup = new Markup($" {text} ", scheme.OffMode);
                    break;

                case PowerSupplyOutputMode.CC:
                case PowerSupplyOutputMode.CV:
                    markup = new Markup($"  {text}  ", scheme.ControlMode);
                    break;

                case PowerSupplyOutputMode.NoInput:
                case PowerSupplyOutputMode.Invalid:
                default:
                    markup = new Markup($" {text} ", scheme.FaultMessage);
                    break;
            }

            return markup;
        }

        private static Markup GetLockStatusMarkup()
        {
            Markup lockStatus =
                wavegenMode ? new Markup(" AWG ", new Style(Color.Black, lockScheme.TableAccent)) :
                ControlsLocked ? new Markup(" LOCKED ", new Style(Color.Black, lockScheme.TableAccent)) :
                    new Markup("        ", new Style(null, null));

            return lockStatus;
        }

        private static Markup GetFaultStatusMarkup(PowerSupplyFaultStatus status)
        {
            Markup faultStatus;

            string text = Enum.GetName(typeof(PowerSupplyFaultStatus), status);

            switch (status)
            {
                case PowerSupplyFaultStatus.OK:
                    faultStatus = new Markup("", new Style(Color.White, null));
                    break;

                case PowerSupplyFaultStatus.OCP:
                case PowerSupplyFaultStatus.OVP:
                case PowerSupplyFaultStatus.OPP:
                case PowerSupplyFaultStatus.OTP:
                case PowerSupplyFaultStatus.UVP:
                case PowerSupplyFaultStatus.REP:
                case PowerSupplyFaultStatus.Invalid:
                default:
                    faultStatus = new Markup($" {text} ", scheme.FaultMessage);
                    break;
            }

            return faultStatus;
        }

        /// <summary>
        /// Gets the data table. This table is responsible for providing the data regularly viewed by the user.
        /// Note that this method is intended to be called in the power supply event handler that is processed
        /// in a thread at regular intervals of 1ms. Some considerations for execution speed should be made
        /// when adding logic to this routine as it may impact the perceived performance of the UI.
        /// </summary>
        /// <param name="supply">The power supply instance.</param>
        /// <param name="setpoint">The current setpoint data.</param>
        /// <param name="system">The current system data.</param>
        /// <param name="active">The current active data.</param>
        /// <returns>The data table containing the key power supply data.</returns>
        private static Table GetDataTable(PowerSupply supply, PowerSupplySetpoint setpoint, PowerSupplySystemParams system, PowerSupplyActiveState active)
        {
            const int numDataRows = 12;
            const int numSeparatorRows = 4;
            const int numHeaderFooterRows = 2;
            const int numExtraRows = 6; // Preset, 2x Empty, V-Row, I-Row, Help Row
            const int totalRows = numDataRows + numSeparatorRows + numHeaderFooterRows + numExtraRows;
            int rowIndex = 0;
            int h = Console.BufferHeight;

            Faulted = active.FaultStatus != PowerSupplyFaultStatus.OK;

            Table tab = new Table()
                .Centered()
                .AddColumn(
                    new TableColumn(GetFaultStatusMarkup(active.FaultStatus))
                        .RightAligned()
                        .Footer(new Markup("Timestamp", scheme.RowHeader)))
                .AddColumn(
                    new TableColumn(new Markup("Setpoint", scheme.ColumnHeader))
                        .Centered()
                        .Footer(""))
                .AddColumn(
                    new TableColumn(new Markup("Actual", scheme.ColumnHeader))
                        .Centered()
                        .Footer(new Markup($"{active.Timestamp.Ticks:X08}", scheme.NumericData).Centered()))
                .AddColumn(
                    new TableColumn(GetLockStatusMarkup())
                        .Alignment(Justify.Left)
                        .Footer(""))
                .AddRow(
                    new Markup("Voltage", scheme.RowHeader),
                    new Markup(GetUserEntryString(setpoint.Voltage, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(active.Voltage), scheme.NumericData),
                    new Markup("mV", scheme.Units))
                .AddRow(
                    new Markup("Current", scheme.RowHeader),
                    new Markup(GetUserEntryString(setpoint.Current, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(active.Current), scheme.NumericData),
                    new Markup("mA", scheme.Units))
                .AddRow(
                    new Markup("Power", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    new Markup(GetUserEntryString((ushort)(active.Voltage * active.Current / 1000)), scheme.NumericData),
                    new Markup("mW", scheme.Units))
                .AddRow(
                    new Markup("OVP", scheme.RowHeader),
                    new Markup(GetUserEntryString(setpoint.OVP, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(supply.PresetParams[supply.Output.Preset].OVP), scheme.NumericData),
                    new Markup("mV", scheme.Units))
                .AddRow(
                    new Markup("OCP", scheme.RowHeader),
                    new Markup(GetUserEntryString(setpoint.OCP, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(supply.PresetParams[supply.Output.Preset].OCP), scheme.NumericData),
                    new Markup("mA", scheme.Units))
                .AddRow(
                    new Markup("OPP", scheme.RowHeader),
                    new Markup(GetUserEntryString(system.OPP, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(supply.SystemParams.OPP), scheme.NumericData),
                    new Markup("dW", scheme.Units))
                .AddRow(
                    new Markup("OTP", scheme.RowHeader),
                    new Markup(GetUserEntryString(system.OTP, (selectedRow == rowIndex++), selectedCol), scheme.NumericData),
                    new Markup(GetUserEntryString(supply.SystemParams.OTP), scheme.NumericData),
                    new Markup(" C", scheme.Units))
                .AddRow(
                    new Markup("V[[usb]]", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    new Markup(GetUserEntryString(active.VoltageUsb5V), scheme.NumericData),
                    new Markup("mV", scheme.Units))
                .AddRow(
                    new Markup("V[[max]]", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    new Markup(GetUserEntryString(active.VoltageOutputMax), scheme.NumericData),
                    new Markup("mV", scheme.Units))
                .AddRow(
                    new Markup("V[[in]]", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    new Markup(GetUserEntryString(active.VoltageInput), scheme.NumericData),
                    new Markup("mV", scheme.Units))
                .AddRow(
                    new Markup("Temp", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    new Markup(GetUserEntryString(active.Temperature), scheme.NumericData),
                    new Markup("dC", scheme.Units))
                .AddRow(
                    new Markup("Mode", scheme.RowHeader),
                    new Markup("---", scheme.VoidData),
                    GetOutputModeMarkup(active.OutputMode),
                    new Text(""))
                .Expand()
                .Border(TableBorder.Horizontal)
                .BorderColor(scheme.TableAccent);

            for (int i = 0; i < h - totalRows; i++)
            {
                tab.AddEmptyRow();
            }

            return tab;
        }

        /// <summary>
        /// Gets the controls panel.
        /// </summary>
        /// <returns>The panel providing user controls information.</returns>
        private static Panel GetControlsPanel()
        {
            return new Panel(
                    Align.Center(GetControlsGrid().Expand(),
                        VerticalAlignment.Middle))
                .Header("[blue] Controls[/] [grey](Press Any Key To Return) [/]")
                .BorderColor(Color.Grey)
                .Expand();
        }

        /// <summary>
        /// Gets the device information panel.
        /// </summary>
        /// <returns>The panel providing device information.</returns>
        private static Panel GetDeviceInfoPanel()
        {
            return new Panel(
                    Align.Center(GetDeviceInfoGrid().Expand(),
                        VerticalAlignment.Middle))
                .Header("[blue] Device Info[/] [grey](Press Any Key To Return) [/]")
                .BorderColor(Color.Grey)
                .Expand();
        }

        /// <summary>
        /// Prompt the user to select from one of the detected devices.
        /// </summary>
        /// <param name="serialNumbers">List of serial numbers to chose from.</param>
        /// <returns>The serial number of the selected device.</returns>
        private static string GetDeviceSelection(List<string> serialNumbers)
        {
            string serialNumber = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Connect to which device (serial number)?")
                    .PageSize(10)
                    .EnableSearch()
                    .MoreChoicesText("[grey](Move up and down to reveal more devices)[/]")
                    .AddChoices(serialNumbers));

            return serialNumber;
        }

        /// <summary>
        /// Get the main grid that presents the power supply data to the user.
        /// </summary>
        /// <param name="supply">The supply instance.</param>
        /// <param name="setpoint">The current setpoint.</param>
        /// <param name="system">The current system parameters.</param>
        /// <param name="active">The active values received by the power supply.</param>
        /// <returns>The grid containing the current state of the device.</returns>
        private static Grid GetDataGrid(PowerSupply supply, PowerSupplySetpoint setpoint, PowerSupplySystemParams system, PowerSupplyActiveState active)
        {
            string controlsCaption = wavegenMode ? "Press Q to Quit." : "Press Q to Quit. Press ? to Show Controls";
            return new Grid()
                .AddColumns(1)
                .AddRow(GetDataTable(supply, setpoint, system, active))
                .AddRow(Align.Center(GetPresetGrid()))
                .AddEmptyRow()
                .AddRow(GetBarChartGrid(active))
                .AddEmptyRow()
                .AddRow(new Markup(controlsCaption, scheme.Caption).Centered());
        }

        /// <summary>
        /// Process keyboard events for extended, more complex key strokes.
        /// </summary>
        /// <param name="key">The keypress information.</param>
        /// <returns>The detected keyboard event.</returns>
        private static KeyboardEvent GetKeyboardEventExtended(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control | ConsoleModifiers.Shift))
            {
                switch (key.Key)
                {
                    case ConsoleKey.L:
                        ControlsLocked ^= true;
                        if (ControlsLocked)
                        {
                            return KeyboardEvent.LockControls;
                        }
                        else
                        {
                            return KeyboardEvent.UnlockControls;
                        }

                    default:
                        return KeyboardEvent.None;
                }
            }
            else if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                switch (key.Key)
                {
                    case ConsoleKey.D0:
                    case ConsoleKey.D1:
                    case ConsoleKey.D2:
                    case ConsoleKey.D3:
                    case ConsoleKey.D4:
                    case ConsoleKey.D5:
                    case ConsoleKey.D6:
                    case ConsoleKey.D7:
                    case ConsoleKey.D8:
                    case ConsoleKey.D9:
                        byte preset = (byte)(key.KeyChar - '0');
                        return (KeyboardEvent.SavePreset0 + preset);

                    default:
                        return KeyboardEvent.None;
                }
            }
            else if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.D2:
                    case ConsoleKey.D3:
                    case ConsoleKey.D4:
                    case ConsoleKey.D5:
                    case ConsoleKey.D6:
                    case ConsoleKey.D7:
                    case ConsoleKey.D8:
                    case ConsoleKey.D9:
                        byte preset = (byte)(key.Key - '0');
                        return (KeyboardEvent.SavePreset0 + preset);

                    default:
                        return KeyboardEvent.None;
                }
            }
            else if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        return KeyboardEvent.IncrementDigit;
                    case ConsoleKey.DownArrow:
                        return KeyboardEvent.DecrementDigit;

                    default:
                        return KeyboardEvent.None;
                }
            }
            else
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        return KeyboardEvent.PreviousControl;
                    case ConsoleKey.DownArrow:
                        return KeyboardEvent.NextControl;
                    case ConsoleKey.RightArrow:
                        return KeyboardEvent.PreviousDigit;
                    case ConsoleKey.LeftArrow:
                        return KeyboardEvent.NextDigit;
                    case ConsoleKey.Enter:
                    default:
                        return KeyboardEvent.None;
                }
            }
        }

        /// <summary>
        /// Processes key press events. To be called regularly by the application task.
        /// </summary>
        /// <returns>The detected keyboard event.</returns>
        private static KeyboardEvent GetKeyboardEvent()
        {
            if (!Console.KeyAvailable)
            {
                return KeyboardEvent.None;
            }

            ConsoleKeyInfo key = Console.ReadKey(true);

            // Flush the input buffer.
            while (Console.KeyAvailable)
                Console.ReadKey(false);

            if (ViewMode == ViewMode.Controls)
            {
                return KeyboardEvent.ReturnToNormal;
            }
            else if (ViewMode == ViewMode.DeviceInfo)
            {
                return KeyboardEvent.ReturnToNormal;
            }
            else if (ViewMode == ViewMode.Protections)
            {
                return KeyboardEvent.ReturnToNormal;
            }
            else if (ControlsLocked == true)
            {
                switch (key.KeyChar)
                {
                    case '\f':
                        // This is related to "Ctrl + Shift + L"
                        return GetKeyboardEventExtended(key);
                    case '?':
                    case '/':
                        return KeyboardEvent.ShowControls;
                    case 'i':
                    case 'I':
                        return KeyboardEvent.ShowDeviceInfo;
                    default:
                        // Ignore all other operations.
                        break;
                }

                return KeyboardEvent.None;
            }
            else
            {
                switch (key.KeyChar)
                {
                    case '\0':
                    case '\f':
                        // This is related to:
                        // Navigation keys, and other keystrokes involving
                        // Control/Shift/Alt. For instance: "Ctrl + Shift + L"
                        return GetKeyboardEventExtended(key);
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
                        {
                            return GetKeyboardEventExtended(key);
                        }
                        else
                        {
                            byte preset = (byte)(key.KeyChar - '0');
                            return (KeyboardEvent.SetPreset0 + preset);
                        }
                    case 'w':
                        return KeyboardEvent.IncrementVoltage;
                    case 'W':
                        return KeyboardEvent.IncrementVoltage10x;
                    case 's':
                        return KeyboardEvent.DecrementVoltage;
                    case 'S':
                        return KeyboardEvent.DecrementVoltage10x;
                    case 'd':
                        return KeyboardEvent.IncrementCurrent;
                    case 'D':
                        return KeyboardEvent.IncrementCurrent10x;
                    case 'a':
                        return KeyboardEvent.DecrementCurrent;
                    case 'A':
                        return KeyboardEvent.DecrementCurrent10x;
                    case '=':
                    case '+':
                        return KeyboardEvent.OutputOn;
                    case '-':
                    case '_':
                        return KeyboardEvent.OutputOff;
                    case '`':
                    case '~':
                        return KeyboardEvent.OutputToggle;
                    case 'q':
                    case 'Q':
                        return KeyboardEvent.Quit;
                    case '?':
                    case '/':
                        return KeyboardEvent.ShowControls;
                    case 'i':
                    case 'I':
                        return KeyboardEvent.ShowDeviceInfo;
                    default:
                        return KeyboardEvent.None;
                }
            }
        }
    }
}
