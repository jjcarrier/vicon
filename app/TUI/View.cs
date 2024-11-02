using LibDP100;
using System;
using Spectre.Console;

namespace PowerSupplyApp
{
    partial class Program
    {
        private static ViewMode ViewMode { get; set; } = ViewMode.Normal;

        private static void EnterAlternateScreenBuffer()
        {
            // Enter alt-screen buffer and hide cursor.
            Console.Write($"{(char)27}[?1049h");
            Console.Write($"{(char)27}[?25l");
            Console.Title = psu.Device.Type;
        }

        private static void ExitAlternateScreenBuffer()
        {
            // Exit alt-screen buffer and show cursor.
            Console.Write($"{(char)27}[?1049l");
            Console.Write($"{(char)27}[?25h");
        }

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

            return $"[white]{markupText}[/]";
        }

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
                .AddRow(new Markup("[blue]i[/]/[blue]I[/]"), new Markup("Device Information"));
        }

        private static Grid GetProtectionsGrid()
        {
            return new Grid()
                .AddColumns(2)
                .AddRow(new Markup("[white]OVP (mV)[/]"), new Markup(GetUserEntryString(sp.OVP, (selectedRow == 0), selectedCol)))
                .AddRow(new Markup("[white]OCP (mA)[/]"), new Markup(GetUserEntryString(sp.OCP, (selectedRow == 1), selectedCol)))
                .AddRow(new Markup("[white]OPP (dW)[/]"), new Markup(GetUserEntryString(sys.OPP, (selectedRow == 2), selectedCol)))
                .AddRow(new Markup("[white]OTP (C)[/]"), new Markup(GetUserEntryString(sys.OTP, (selectedRow == 3), selectedCol)));
        }

        private static BreakdownChart GetBreakdown100(int actual, int setpoint, int limit)
        {
            BreakdownChart breakdown;

            //if (setpoint < limit)
            //{
            //    int s = (limit == 0) ? 100 : 100 * setpoint / limit;
            //    int a = (setpoint == 0) ? 0 : 100 * actual / setpoint;
            //    breakdown = new BreakdownChart()
            //        .HideTags()
            //        .AddItem("ACT", a, Color.DarkRed)
            //        .AddItem("SET", s - a, Color.White)
            //        .AddItem("LIM", 100 - s, Color.Grey);
            //}
            //else
            {
                int a = (limit == 0) ? 100 : 100 * actual / limit;
                breakdown = new BreakdownChart()
                    .HideTags()
                    .AddItem("ACT", a, Color.DarkRed)
                    .AddItem("LIM", 100 - a, Color.Grey);
            }

            return breakdown;
        }

        private static Grid GetBarChartGrid(PowerSupplyActuals actual)
        {
            int vo_set = sp.Voltage;
            int vo_limit = (actual.VoltageOutputMax > psu.PresetParams[psu.Output.Preset].OVP) ?
                psu.PresetParams[psu.Output.Preset].OVP : actual.VoltageOutputMax;
            int io_set = sp.Current;
            int io_limit = psu.PresetParams[psu.Output.Preset].OCP;
            int v = (vo_limit == 0) ? 100 : 100 * actual.Voltage / vo_limit;
            int i = (io_limit == 0) ? 100 : 100 * actual.Current / io_limit;

            BreakdownChart vBreakdown = GetBreakdown100(actual.Voltage, vo_set, vo_limit);
            BreakdownChart iBreakdown = GetBreakdown100(actual.Current, io_set, io_limit);

            return new Grid()
                .AddColumns(2)
                .Centered()
                .AddRow(new Text("V"), vBreakdown)
                .AddRow(new Text("I"), iBreakdown);
        }

        private static Grid GetPresetGrid()
        {
            Markup[] presetText =
            {
                new Markup(" 1 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 2 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 3 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 4 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 5 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 6 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 7 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 8 ", new Style(Color.Black, Color.Grey)),
                new Markup(" 9 ", new Style(Color.Black, Color.Grey))
            };

            if (psu.Output.Preset > 0)
            {
                presetText[psu.Output.Preset - 1] = new Markup($" {psu.Output.Preset} ", new Style(Color.White, Color.DarkRed));
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
                    markup = new Markup($"[dim]{text}[/]");
                    break;

                case PowerSupplyOutputMode.CC:
                case PowerSupplyOutputMode.CV:
                    markup = new Markup($" {text} ", new Style(Color.Black, Color.Grey));
                    break;

                case PowerSupplyOutputMode.NoInput:
                case PowerSupplyOutputMode.Invalid:
                default:
                    markup = new Markup($" {text} ", new Style(Color.White, Color.DarkRed));
                    break;
            }

            return markup;
        }

        private static Markup GetFaultStatusMarkup(PowerSupplyFaultStatus status)
        {
            Markup markup;
            string text = Enum.GetName(typeof(PowerSupplyFaultStatus), status);

            switch (status)
            {
                case PowerSupplyFaultStatus.OK:
                    markup = new Markup(wavegenMode ? " AWG " : "", new Style(Color.Black, Color.Gold1));
                    break;

                case PowerSupplyFaultStatus.OCP:
                case PowerSupplyFaultStatus.OVP:
                case PowerSupplyFaultStatus.OPP:
                case PowerSupplyFaultStatus.OTP:
                case PowerSupplyFaultStatus.UVP:
                case PowerSupplyFaultStatus.REP:
                case PowerSupplyFaultStatus.Invalid:
                default:
                    markup = new Markup($" {text} ", new Style(Color.White, Color.DarkRed));
                    break;
            }

            return markup;
        }

        private static Table GetDataTable(PowerSupply supply, PowerSupplySetpoint setpoint, PowerSupplySystemParams system, PowerSupplyActuals actual)
        {
            int h = Console.BufferHeight;
            Table tab = new Table()
                .Centered()
                .AddColumn(new TableColumn(GetFaultStatusMarkup(actual.FaultStatus)).RightAligned().Footer(new Markup("[white]Timestamp[/]")))
                .AddColumn(new TableColumn(new Markup("[white]Setpoint[/]")).Centered().Footer(""))
                .AddColumn(new TableColumn(new Markup("[white]Actual[/]")).Centered().Footer(new Text($"{actual.Timestamp.Ticks:X08}").Centered()))
                .AddColumn(new TableColumn(new Text("")).Alignment(Justify.Left).Footer(""))
                .AddRow(new Markup("[white]Voltage[/]"), new Markup(GetUserEntryString(setpoint.Voltage, (selectedRow == 0), selectedCol)), new Markup(GetUserEntryString(actual.Voltage)), new Text("mV"))
                .AddRow(new Markup("[white]Current[/]"), new Markup(GetUserEntryString(setpoint.Current, (selectedRow == 1), selectedCol)), new Markup(GetUserEntryString(actual.Current)), new Text("mA"))
                .AddRow(new Markup("[white]Power[/]"), new Text("---"), new Markup(GetUserEntryString((ushort)(actual.Voltage * actual.Current / 1000))), new Text("mW"))
                .AddRow(new Markup("[white]OVP[/]"), new Markup(GetUserEntryString(setpoint.OVP, (selectedRow == 2), selectedCol)), new Markup(GetUserEntryString(supply.PresetParams[supply.Output.Preset].OVP)), new Text("mV"))
                .AddRow(new Markup("[white]OCP[/]"), new Markup(GetUserEntryString(setpoint.OCP, (selectedRow == 3), selectedCol)), new Markup(GetUserEntryString(supply.PresetParams[supply.Output.Preset].OCP)), new Text("mA"))
                .AddRow(new Markup("[white]OPP[/]"), new Markup(GetUserEntryString(system.OPP, (selectedRow == 4), selectedCol)), new Markup(GetUserEntryString(supply.SystemParams.OPP)), new Text("dW"))
                .AddRow(new Markup("[white]OTP[/]"), new Markup(GetUserEntryString(system.OTP, (selectedRow == 5), selectedCol)), new Markup(GetUserEntryString(supply.SystemParams.OTP)), new Text("C"))
                .AddRow(new Markup("[white]V[[usb]][/]"), new Text("---"), new Markup(GetUserEntryString(actual.VoltageUsb5V)), new Text("mV"))
                .AddRow(new Markup("[white]V[[max]][/]"), new Text("---"), new Markup(GetUserEntryString(actual.VoltageOutputMax)), new Text("mV"))
                .AddRow(new Markup("[white]V[[in]][/]"), new Text("---"), new Markup(GetUserEntryString(actual.VoltageInput)), new Text("mV"))
                .AddRow(new Markup("[white]Mode[/]"), new Text("---"), GetOutputModeMarkup(actual.OutputMode), new Text(""))
                .Expand()
                .Border(TableBorder.Horizontal)
                .BorderColor(Color.Grey);

            for (int i = 0; i < h - 23; i++)
            {
                tab.AddEmptyRow();
            }

            return tab;
        }

        private static Panel GetControlsPanel()
        {
            return new Panel(
                    Align.Center(GetControlsGrid().Expand(),
                        VerticalAlignment.Middle))
                .Header("[blue] Controls[/] [grey](Press Any Key To Return) [/]")
                .BorderColor(Color.Grey)
                .Expand();
        }

        private static Panel GetDeviceInfoPanel()
        {
            return new Panel(
                    Align.Center(GetDeviceInfoGrid().Expand(),
                        VerticalAlignment.Middle))
                .Header("[blue] Device Info[/] [grey](Press Any Key To Return) [/]")
                .BorderColor(Color.Grey)
                .Expand();
        }

        private static Grid GetDataGrid(PowerSupply supply, PowerSupplySetpoint setpoint, PowerSupplySystemParams system, PowerSupplyActuals actual)
        {
            return new Grid()
                .AddColumns(1)
                .AddRow(GetDataTable(supply, setpoint, system, actual))
                .AddRow(Align.Center(GetPresetGrid()))
                .AddEmptyRow()
                .AddRow(GetBarChartGrid(actual))
                .AddEmptyRow()
                .AddRow(new Markup("[dim]Press Q to Quit. Press ? to Show Controls[/]").Centered());
        }

        private static KeyboardEvent GetKeyboardEventExtended(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
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
            else
            {
                switch (key.KeyChar)
                {
                    case '\0':
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
