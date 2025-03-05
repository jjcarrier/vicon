function Get-PsuSerialNumbers()
{
    $devices = vicon --enumerate --json | ConvertFrom-Json
    $devices | ForEach-Object { $_.Response.Device.SerialNumber }
}

$viconScriptBlock = {
    param($wordToComplete, $commandAst, $cursorPosition)

    $helpData = Invoke-Expression 'vicon -?'
    $paramValueAssign = $wordToComplete.Contains('=') -and $wordToComplete.IndexOf('=') -lt $cursorPosition
    if ($wordToComplete.StartsWith("--") -and -not $paramValueAssign) {
        Get-ParsedHelpOption -HelpData $helpData |
            New-ParsedHelpParamCompletionResult -WordToComplete $wordToComplete
    } elseif ($wordToComplete.StartsWith("-") -and -not $paramValueAssign) {
        Get-ParsedHelpFlag -HelpData $helpData |
            New-ParsedHelpParamCompletionResult -WordToComplete $wordToComplete
    } else {

        # If the previous option flag is "--serial" or "--sn" then create completion results
        # for each item returned in Get-PsuSerialNumbers().
        $previousFlag = $commandAst.CommandElements |
            Where-Object { $_.Extent.EndOffset -lt $cursorPosition } |
            Select-Object -Last 1

        if ($previousFlag -and ($previousFlag -match '--serial|--sn')) {
            Get-PsuSerialNumbers | Where-Object { $_ -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        } elseif ($previousFlag -and ($previousFlag -match '--theme')) {
            @('classic', 'black-and-white', 'grey', 'dark-red', 'dark-green', 'dark-magenta', 'cyan', 'gold', 'blue', 'blue-violet') |
                Where-Object { $_ -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        } else {
            $resultPrefix = ''
            $values = $helpData |
                Get-ParsedHelpParamValue `
                    -WordToComplete $wordToComplete `
                    -CommandAst $commandAst `
                    -CursorPosition $cursorPosition `
                    -ParamValueAssignment:$paramValueAssign `
                    -ResultPrefix ([ref]$resultPrefix)
            $values | New-ParsedHelpValueCompletionResult -ResultPrefix $resultPrefix
        }
    }
}

Register-ArgumentCompleter -CommandName 'vicon' -Native -ScriptBlock $viconScriptBlock
