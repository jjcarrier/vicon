# VICON: Power Controller

> [!WARNING]
> This repository is in an early release state and is subject to breaking
> changes, faulty behavior, and/or incomplete functionality. Please use extra
> caution while in this state. There is no guarantee that the functionality
> currently provided will remain compatible/available in future revisions. With
> that said, feedback/suggestions/bug reports are very much welcome and should
> be issued via a GitHub issue. It is highly recommended to review the issues
> page prior to use to understand any known issues with the tool so you may
> assess the risk of using this while in this early release state.

<!-- markdownlint-disable -->
<p align="center">
  <img
    width="300"
    src="images/drawing.svg"
    alt="vicon – A power controller console"
  />
</p>
<!-- markdownlint-enable -->

## Description

`vicon` : (vī'con) Short for V-I-Console and a play on the pronunciation of viking.

A CLI/TUI for controlling the AlienTek DP100 (ATK-DP100) over USB.

## Checklist / Features

Below is a basic overview of `vicon` functionality:

- [x] Supports a CLI Interface
  - Serial processing of commands, allowing for complex control sequences.
  - Normal-style and JSON-style output to provide option for human readability
    and tool integration.
- [x] Support a Text-UI (TUI)
  - A streamlined text-based user interface providing control with minimal and
    intuitive keystrokes.
- [x] Waveform Generator (AWG)
  - A basic JSON file format is used to describe setpoint sequences for
    generating arbitrary waveforms (low speed).
- [x] Suppressed ATK-DP100DLL debug output
- [x] Wireshark USB dissector
- [x] PowerShell tab-completions
- [x] JSON output
  - Providing a convenient way to integrate with other tooling.
- [x] Serial command line processing
- [ ] Recorder/trace (support for logging all activity during interactive mode)
- [ ] Supports a TUI lock function to prevent accidental button presses

## Software Requirements

- Requires .NET Framework (v4.8.1)
  - A future version will hopefully switch to .NET Core to make this tool
    cross-platform.

## Initial Setup

Checkout and configure the repository via:

```pwsh
git clone https://github.com/jjcarrier/vicon.git
cd vicon
dotnet restore
```

Build the project via:

```pwsh
dotnet build
```

Add the build directory to your PowerShell `$PROFILE` to make the command
available from any directory.

Run the follow command for help documentation:

```pwsh
vicon --help
```

## CLI Examples

<details>
  <summary>Read Output Configs</summary>

This example shows how to read out the various configurable parameters of the device.

```pwsh
vicon --read-sys --read-out --read-pre 0 10 --json
```

```output
{"Command":"ReadSystem","Response":{"SystemParams":{"OPP":30,"OTP":60,"RPP":true,"AutoOn":false,"Backlight":1,"Volume":2}}}
{"Command":"ReadOutput","Response":{"Output":{"On":false,"Preset":0,"Setpoint":{"Voltage":3300,"Current":300,"OVP":8125,"OCP":250}}}}
{"Command":"ReadPreset","Response":{"Index":0,"Preset":{"Voltage":3300,"Current":300,"OVP":8125,"OCP":250}}}
{"Command":"ReadPreset","Response":{"Index":1,"Preset":{"Voltage":3300,"Current":200,"OVP":3500,"OCP":500}}}
...
{"Command":"ReadPreset","Response":{"Index":9,"Preset":{"Voltage":10000,"Current":5000,"OVP":30500,"OCP":5050}}}
```

</details>

<details>
  <summary>Basic Control</summary>

This example sets the output to 3.3V and a max current of 500mA. Afterwards,
delay for 500ms and then turn the output on, delay for 5 seconds and finally
turn the output off. All commands and responses are output in JSON format.

```pwsh
vicon --mv 3300 --ma 500 --delay 500 --on --delay 5000 --off --json
```

```output
{"Command":"WriteVoltage","Response":{"Voltage":3300,"Current":500,"OVP":8125,"OCP":250}}
{"Command":"WriteCurrent","Response":{"Voltage":3300,"Current":500,"OVP":8125,"OCP":250}}
{"Command":"WriteOutputOn","Response":{"On":true}}
{"Command":"WriteOutputOff","Response":{"On":false}}
```

</details>

<details>
  <summary>Monitoring Output</summary>

This example is a more involved example showing how one may read data from the
JSON output and convert it into a tabular form using PowerShell cmdlets. This
primarily serves to illustrate how PowerShell may be used as glue logic between
two CLI utilities to perform a more complicated task.

```pwsh
vicon --off --delay 1000 --mv 7500 --ma 400 --on --ra 50 1 --json | `
  ForEach-Object { $_ | ConvertFrom-Json } | `
  Where-Object { $_.Command -eq 'ReadActuals' } | `
  Select-Object -ExpandProperty Response | `
  Select-Object -ExpandProperty ActualOutput | `
  Select-Object -Property Timestamp,FaultStatus,OutputMode,Voltage,Current | `
  Format-Table
```

```output
Timestamp        FaultStatus OutputMode Voltage Current
---------        ----------- ---------- ------- -------
11:50:50.6286131           0          0     285      61
11:50:50.6421596           0          1    6432     188
11:50:50.6574657           0          1    7087     148
11:50:50.6719755           0          1    7296     108
11:50:50.6871800           0          1    7399      79
11:50:50.7029259           0          1    7450      56
11:50:50.7182109           0          1    7472      41
11:50:50.7325633           0          1    7482      30
11:50:50.7484842           0          1    7486      23
11:50:50.7635261           0          1    7488      18
11:50:50.7795500           0          1    7490      14
11:50:50.7959834           0          1    7491      17
11:50:50.8123372           0          1    7492      19
...
```

</details>

## Interactive Mode (TUI)

A fully featured text user interface (TUI) is available via the `--interactive`
option.

```pwsh
vicon --interactive
```

<!-- markdownlint-disable -->
<p align="center">
  <img
    src="images/interactive-mode.gif"
    alt="interactive mode"
  />
</p>
<!-- markdownlint-enable -->

> [!IMPORTANT]
> The `ctrl+c` keystroke will immediately exit interactive-mode but will allow
> for post-interactive commands to execute. This way, a safe series of operations
> may be performed at conclusion of this mode regardless of whether the user
> exits with `q` keystroke or `ctrl+c`.

## Additional

### Tab-Completions

A tab-completer is available in the `completion` folder, offerring an improved
user-experience. This module leverages [PS-HelpParser](https://github.com/jjcarrier/PS-HelpParser)
to automatically parse the help documentation and provide tab-completion results
to the user.

The completion file is provided in:

```pwsh
./completion/VIConCompletion.psm1
```

With the `HelpParser` module installed via
[PSGallery](https://www.powershellgallery.com/packages/HelpParser) import both
the parser and the completion module via:

```pwsh
Import-Module HelpParser
Import-Module <full_path_to_file>/VIConCompletion.psm1
```

Add the above to your `$PROFILE` to make the changes persist between sessions.

### WireShark Dissector

A Lua dissector is provided in the `./wireshark/` directory.
This dissector offers a way to debug and investigate behavior of the ATK-DP100's
HID interface.

## License

Copyright © Jon Carrier

This project is licensed under the MIT license. For more details please refer to
[LICENSE](./LICENSE). This software depends on the following third party
components:

- AlientTek's ATK-DP100DLL
- Spectre.Console (https://github.com/spectreconsole/spectre.console/LICENSE.md)
- Newtonsoft.Json (https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
