#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="${VICON_INSTALL_DIR:-/opt/vicon}"
UDEV_RULE_NAME="99-atk-dp100.rules"
PATH_BEGIN="# vicon path begin"
PATH_END="# vicon path end"

remove_path_block() {
    local rc_file="$1"
    if [[ -z "$rc_file" || ! -f "$rc_file" ]]; then
        return 0
    fi

    if ! grep -q "$PATH_BEGIN" "$rc_file"; then
        return 0
    fi

    local tmp_file
    tmp_file="$(mktemp)"
    awk -v begin="$PATH_BEGIN" -v end="$PATH_END" '
        $0 == begin {skip=1; next}
        $0 == end {skip=0; next}
        skip != 1 {print}
    ' "$rc_file" > "$tmp_file"
    mv "$tmp_file" "$rc_file"
}

remove_bash_completion() {
    local target_dirs=(
        "/etc/bash_completion.d"
        "/usr/local/etc/bash_completion.d"
        "/opt/homebrew/etc/bash_completion.d"
    )

    for dir in "${target_dirs[@]}"; do
        if [[ -f "$dir/vicon" ]]; then
            sudo rm -f "$dir/vicon"
        fi
    done
}

remove_udev_rules() {
    if [[ "$(uname -s)" != "Linux" ]]; then
        return 0
    fi

    if [[ -f "/etc/udev/rules.d/$UDEV_RULE_NAME" ]]; then
        sudo rm -f "/etc/udev/rules.d/$UDEV_RULE_NAME"
        sudo udevadm control --reload-rules
        sudo udevadm trigger
    fi
}

if [[ -d "$INSTALL_DIR" ]]; then
    sudo rm -rf "$INSTALL_DIR"
fi

remove_path_block "$HOME/.bashrc"
remove_path_block "$HOME/.zshrc"
remove_path_block "$HOME/.config/fish/config.fish"

remove_udev_rules
remove_bash_completion

echo "Uninstall complete. Restart your shell to pick up PATH changes."
