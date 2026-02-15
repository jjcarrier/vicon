#!/usr/bin/env bash

_vicon_get_serials() {
	local prefix="$1"
	local py_cmd=""

	if command -v python3 >/dev/null 2>&1; then
		py_cmd="python3"
	elif command -v python >/dev/null 2>&1; then
		py_cmd="python"
	else
		return 0
	fi

	"$py_cmd" - "$prefix" <<'PY'
import json
import subprocess
import sys

prefix = sys.argv[1]
try:
	output = subprocess.check_output(["vicon", "--enumerate", "--json"], text=True)
except Exception:
	sys.exit(0)

serials = []
for line in output.splitlines():
	line = line.strip()
	if not line:
		continue
	try:
		payload = json.loads(line)
	except Exception:
		continue
	device = payload.get("Response", {}).get("Device", {})
	serial = str(device.get("SerialNumber", "")).strip()
	if serial and prefix in serial:
		serials.append(serial)

print(" ".join(serials))
PY
}

_vicon_complete() {
	local cur prev
	COMPREPLY=()
	cur="${COMP_WORDS[COMP_CWORD]}"
	prev="${COMP_WORDS[COMP_CWORD-1]}"

	local options="--help -h -? --version -v --config --save --load --check --debug --enumerate --blink --serial --sn --interactive --theme --json --json-list --delay -d --preset -p --recall -r --wavegen --awg --read-dev --rd --read-sys --rs --read-out --ro --read-act --ra --read-pre --rp --on --off --millivolts --mv --milliamps --ma --ovp --ocp --opp --otp --rpp --auto-on --volume --backlight"
	local themes="classic black-and-white grey dark-red dark-green dark-magenta cyan gold blue blue-violet"

	case "$prev" in
		--serial|--sn)
			COMPREPLY=( $(compgen -W "$(_vicon_get_serials "$cur")" -- "$cur") )
			return 0
			;;
		--theme)
			COMPREPLY=( $(compgen -W "$themes" -- "$cur") )
			return 0
			;;
		--wavegen|--awg)
			compopt -o filenames 2>/dev/null
			COMPREPLY=( $(compgen -f -- "$cur") )
			return 0
			;;
	esac

	if [[ "$cur" == -* ]]; then
		COMPREPLY=( $(compgen -W "$options" -- "$cur") )
		return 0
	fi

	return 0
}

complete -F _vicon_complete vicon
