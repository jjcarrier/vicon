using LibDP100;
using PowerSupplyApp.TUI;
using Spectre.Console;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        // The theme that is actively being applied to the TUI.
        private static ColorTheme theme = ColorThemes.Classic;

        // The scheme that is actively being applied to the TUI.
        private static ColorScheme scheme = ColorThemes.Classic.Normal;

        // The current view mode of the TUI.
        private static ViewMode ViewMode { get; set; } = ViewMode.Normal;

        private static NumericDataString voltageEntry =  new NumericDataString(6, 3);
        private static NumericDataString voltageActive = new NumericDataString(6, 3);
        private static NumericDataString voltageLimit= new NumericDataString(6, 3);

        private static NumericDataString currentEntry = new NumericDataString(6, 3);
        private static NumericDataString currentActive = new NumericDataString(6, 3);
        private static NumericDataString currentLimit = new NumericDataString(6, 3);

        private static NumericDataString powerActive = new NumericDataString(6, 2);
        private static NumericDataString powerLimit = new NumericDataString(6, 1);

        private static NumericDataString tempActive = new NumericDataString(6, 1);
        private static NumericDataString tempLimit = new NumericDataString(5);

        private static NumericDataString voltageUsb = new NumericDataString(6, 3);
        private static NumericDataString voltageMax = new NumericDataString(6, 3);
        private static NumericDataString voltageInput = new NumericDataString(6, 3);

        private static bool Faulted
        {
            get { return faulted; }
            set
            {
                faulted = value;
                if (faulted)
                {
                    scheme = theme.Faulted;
                }
                else if (ControlsLocked)
                {
                    scheme = theme.Locked;
                }
                else
                {
                    scheme = theme.Normal;
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
                    scheme = theme.Faulted;
                }
                else if (ControlsLocked)
                {
                    scheme = theme.Locked;
                }
                else
                {
                    scheme = theme.Normal;
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
                .AddRow(new Markup("[blue]0[/]-[blue]9[/]"), new Markup("Preset Selection"))
                .AddRow(new Markup("[blue]Alt[/] + [blue]0[/]-[blue]9[/]"), new Markup("Alter/Store Current Setpoints to Preset"))
                .AddRow(new Markup("[blue]↑[/]/[blue]↓[/]/[blue]←[/]/[blue]→[/]"), new Markup("Entry Navigation"))
                .AddRow(new Markup("[blue]Shift[/] + [blue]↑[/]/[blue]↓[/]"), new Markup("Digit Modification"))
                .AddRow(new Markup("[blue]Control[/] + [blue]Shift[/] + [blue]L[/]"), new Markup("Lock/Unlock Device Controls"))
                .AddRow(new Markup("[blue]i[/]/[blue]I[/]"), new Markup("Device Information"))
                .AddRow(new Markup("[blue]q[/]/[blue]Q[/]"), new Markup("Quit Application"))
                .AddRow(new Markup("[blue]/[/]/[blue]?[/]"), new Markup("Show Controls"));
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
            int vo_limit = (activeState.VoltageOutputMax > psu.Presets[psu.Output.Preset].OVP) ?
                psu.Presets[psu.Output.Preset].OVP : activeState.VoltageOutputMax;
            int io_limit = psu.Presets[psu.Output.Preset].OCP;

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
                new Markup(" 0 ", scheme.Preset),
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

            presetText[psu.Output.Preset] = new Markup($" {psu.Output.Preset} ", scheme.PresetSelected);

            return new Grid()
                .AddColumns(10)
                .AddRow(presetText[0],
                        presetText[1],
                        presetText[2],
                        presetText[3],
                        presetText[4],
                        presetText[5],
                        presetText[6],
                        presetText[7],
                        presetText[8],
                        presetText[9]);
        }

        private static Markup GetOutputModeMarkup(PowerSupplyOutputMode mode)
        {
            Markup markup;
            string text = Enum.GetName(typeof(PowerSupplyOutputMode), mode);

            switch (mode)
            {
                case PowerSupplyOutputMode.OFF:
                    markup = new Markup($"   {text}", scheme.OffMode);
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
                wavegenMode ? new Markup(" AWG ", scheme.UserControl) :
                ControlsLocked ? new Markup(" LOCKED ", scheme.UserControl) :
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
            const string voidDataString = "----";
            const int numDataRows = 8;
            const int numSeparatorRows = 4;
            const int numHeaderFooterRows = 1;
            const int numExtraRows = 5; // Preset, 1x Empty, V-Row, I-Row, Help Row
            const int totalRows = numDataRows + numSeparatorRows + numHeaderFooterRows + numExtraRows;
            int rowIndex = 0;
            int h = Console.BufferHeight;

            Faulted = active.FaultStatus != PowerSupplyFaultStatus.OK;

            bool ovpModified = setpoint.OVP != supply.Presets[supply.Output.Preset].OVP;
            bool ocpModified = setpoint.OCP != supply.Presets[supply.Output.Preset].OCP;
            bool oppModified = system.OPP != supply.SystemParams.OPP;
            bool otpModified = system.OTP != supply.SystemParams.OTP;

            voltageEntry.UpdateValue(setpoint.Voltage);
            voltageEntry.Selected = (selectedRow == rowIndex++);
            voltageEntry.SelectedDigitIndex = selectedCol;

            currentEntry.UpdateValue(setpoint.Current);
            currentEntry.Selected = (selectedRow == rowIndex++);
            currentEntry.SelectedDigitIndex = selectedCol;

            voltageActive.UpdateValue(active.Voltage);
            currentActive.UpdateValue(active.Current);
            powerActive.UpdateValue((ushort)(active.Voltage * active.Current / 10000));
            tempActive.UpdateValue(active.Temperature1);

            voltageLimit.UpdateValue(setpoint.OVP);
            voltageLimit.Selected = (selectedRow == rowIndex++);
            voltageLimit.SelectedDigitIndex = selectedCol;
            voltageLimit.Modified = ovpModified;

            currentLimit.UpdateValue(setpoint.OCP);
            currentLimit.Selected = (selectedRow == rowIndex++);
            currentLimit.SelectedDigitIndex = selectedCol;
            currentLimit.Modified = ocpModified;

            powerLimit.UpdateValue(system.OPP);
            powerLimit.Selected = (selectedRow == rowIndex++);
            powerLimit.SelectedDigitIndex = selectedCol;
            powerLimit.Modified = oppModified;

            tempLimit.UpdateValue(system.OTP);
            tempLimit.Selected = (selectedRow == rowIndex++);
            tempLimit.SelectedDigitIndex = selectedCol;
            tempLimit.Modified = otpModified;

            voltageUsb.UpdateValue(active.VoltageUsb5V);
            voltageMax.UpdateValue(active.VoltageOutputMax);
            voltageInput.UpdateValue(active.VoltageInput);

            Table tab = new Table()
                .Centered()
                .AddColumn(
                    new TableColumn(GetFaultStatusMarkup(active.FaultStatus))
                        .RightAligned())
                .AddColumn(
                    new TableColumn(new Markup("Setpoint", scheme.ColumnHeader))
                        .Centered())
                .AddColumn(
                    new TableColumn(new Markup(" Limit", scheme.ColumnHeader))
                        .Centered())
                .AddColumn(
                    new TableColumn(new Markup("Actual", scheme.ColumnHeader))
                        .Centered())
                .AddColumn(
                    new TableColumn(GetLockStatusMarkup())
                        .Alignment(Justify.Left))
                .AddRow(
                    new Markup("Voltage", scheme.RowHeader),
                    new Markup(voltageEntry.ToMarkupString(), scheme.NumericData),
                    new Markup(voltageLimit.ToMarkupString(), scheme.NumericData),
                    new Markup(voltageActive.ToMarkupString(), scheme.NumericData),
                    new Markup("V", scheme.Units))
                .AddRow(
                    new Markup("Current", scheme.RowHeader),
                    new Markup(currentEntry.ToMarkupString(), scheme.NumericData),
                    new Markup(currentLimit.ToMarkupString(), scheme.NumericData),
                    new Markup(currentActive.ToMarkupString(), scheme.NumericData),
                    new Markup("A", scheme.Units))
                .AddRow(
                    new Markup("Power", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(powerLimit.ToMarkupString(), scheme.NumericData),
                    new Markup(powerActive.ToMarkupString(), scheme.NumericData),
                    new Markup("W", scheme.Units))
                .AddRow(
                    new Markup("Temp", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup($"{tempLimit.ToMarkupString()}.", scheme.NumericData),
                    new Markup(tempActive.ToMarkupString(), scheme.NumericData),
                    new Markup("C", scheme.Units))
                .AddRow(
                    new Markup("V[[usb]]", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voltageUsb.ToMarkupString(), scheme.NumericData),
                    new Markup("V", scheme.Units))
                .AddRow(
                    new Markup("V[[max]]", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voltageMax.ToMarkupString(), scheme.NumericData),
                    new Markup("V", scheme.Units))
                .AddRow(
                    new Markup("V[[in]]", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voltageInput.ToMarkupString(), scheme.NumericData),
                    new Markup("V", scheme.Units))
                .AddRow(
                    new Markup("Mode", scheme.RowHeader),
                    new Markup(voidDataString, scheme.VoidData),
                    new Markup(voidDataString, scheme.VoidData),
                    GetOutputModeMarkup(active.OutputMode),
                    new Text(""))
                .Expand()
                .Border(TableBorder.Horizontal)
                .BorderStyle(scheme.TableAccent);

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
            string controlsCaption = wavegenMode ? "Press Q to Quit." : "Press Q to Quit. Press ? to Show Controls.";
            return new Grid()
                .AddColumns(1)
                .AddRow(GetDataTable(supply, setpoint, system, active))
                .AddRow(Align.Center(GetPresetGrid()))
                .AddRow(new Rule().RuleStyle(scheme.TableAccent))
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
