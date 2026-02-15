#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="${VICON_INSTALL_DIR:-/opt/vicon}"
UDEV_RULE_NAME="99-atk-dp100.rules"
PATH_BEGIN="# vicon path begin"
PATH_END="# vicon path end"

confirm() {
	local prompt="$1"
	read -r -p "$prompt [y/N] " reply
	[[ "${reply,,}" == "y" || "${reply,,}" == "yes" ]]
}

detect_shell_rc() {
	case "${SHELL:-}" in
		*/bash) echo "$HOME/.bashrc" ;;
		*/zsh) echo "$HOME/.zshrc" ;;
		*/fish) echo "$HOME/.config/fish/config.fish" ;;
		*) echo "" ;;
	esac
}

add_path_entry() {
	local rc_file="$1"
	if [[ -z "$rc_file" ]]; then
		echo "Unsupported shell for PATH setup. Please add $INSTALL_DIR to PATH manually."
		return 0
	fi

	if [[ ! -f "$rc_file" ]]; then
		mkdir -p "$(dirname "$rc_file")"
		touch "$rc_file"
	fi

	if grep -q "$PATH_BEGIN" "$rc_file"; then
		return 0
	fi

	if [[ "$rc_file" == *"/config.fish" ]]; then
		{
			echo "$PATH_BEGIN"
			echo "set -gx PATH $INSTALL_DIR \$PATH"
			echo "$PATH_END"
		} >> "$rc_file"
	else
		{
			echo "$PATH_BEGIN"
			echo "export PATH=\"$INSTALL_DIR:\$PATH\""
			echo "$PATH_END"
		} >> "$rc_file"
	fi
}

install_bash_completion() {
	local completion_src="$SCRIPT_DIR/completion/VIConCompletion.sh"
	if [[ ! -f "$completion_src" ]]; then
		echo "Bash completion file not found at $completion_src"
		return 0
	fi

	local target_dir=""
	if [[ "$(uname -s)" == "Linux" ]]; then
		target_dir="/etc/bash_completion.d"
	else
		if [[ -d "/opt/homebrew/etc/bash_completion.d" ]]; then
			target_dir="/opt/homebrew/etc/bash_completion.d"
		else
			target_dir="/usr/local/etc/bash_completion.d"
		fi
	fi

	if confirm "Install bash-completion to $target_dir?"; then
		sudo mkdir -p "$target_dir"
		sudo install -m 644 "$completion_src" "$target_dir/vicon"
	else
		echo "Skipped bash-completion installation."
	fi
}

install_udev_rules() {
	if [[ "$(uname -s)" != "Linux" ]]; then
		echo "Skipping udev rules (not Linux)."
		return 0
	fi

	local udev_src="$SCRIPT_DIR/udev/$UDEV_RULE_NAME"
	if [[ ! -f "$udev_src" ]]; then
		echo "Udev rule file not found at $udev_src"
		return 0
	fi

	if confirm "Install udev rules to /etc/udev/rules.d?"; then
		sudo install -m 644 "$udev_src" "/etc/udev/rules.d/$UDEV_RULE_NAME"
		sudo udevadm control --reload-rules
		sudo udevadm trigger
	else
		echo "Skipped udev rules installation."
	fi
}

if [[ -d "$INSTALL_DIR" ]]; then
	if confirm "Existing installation found at $INSTALL_DIR. Uninstall first?"; then
		VICON_INSTALL_DIR="$INSTALL_DIR" bash "$SCRIPT_DIR/uninstall.sh"
	else
		echo "Installation aborted."
		exit 1
	fi
fi

if [[ ! -d "$SCRIPT_DIR/vicon" ]]; then
	echo "Missing vicon build directory. Run this script from the extracted tar root."
	exit 1
fi

sudo mkdir -p "$INSTALL_DIR"
sudo cp -R "$SCRIPT_DIR/vicon/." "$INSTALL_DIR/"
sudo chmod 755 "$INSTALL_DIR/vicon"

if confirm "Add $INSTALL_DIR to PATH in your shell config?"; then
	add_path_entry "$(detect_shell_rc)"
else
	echo "Skipped PATH update."
fi

install_udev_rules
install_bash_completion

echo "Installation complete. Restart your shell to pick up PATH changes."
