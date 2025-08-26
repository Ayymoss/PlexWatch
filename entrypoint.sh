#!/bin/sh

#
# --- User & Group Setup ---
#
USER_ID=${PUID:-0}
GROUP_ID=${PGID:-0}

if [ "$(id -u)" = "0" ] && [ "$USER_ID" -ne 0 ]; then
    echo "Running as user: $USER_ID:$GROUP_ID"

    GROUPNAME="appgroup"
    if getent group "$GROUP_ID" >/dev/null; then
        GROUPNAME=$(getent group "$GROUP_ID" | cut -d: -f1)
        echo "Group with GID $GROUP_ID already exists, adopting name '$GROUPNAME'"
    else
        addgroup --gid "$GROUP_ID" "$GROUPNAME"
    fi

    USERNAME="appuser"
    if getent passwd "$USER_ID" >/dev/null; then
        USERNAME=$(getent passwd "$USER_ID" | cut -d: -f1)
        echo "User with UID $USER_ID already exists, adopting name '$USERNAME'"
    else
        adduser --system --uid "$USER_ID" --gid "$GROUP_ID" --shell /sbin/nologin "$USERNAME"
    fi

    echo "Setting ownership for $USERNAME:$GROUPNAME..."
    chown -R "$USERNAME":"$GROUPNAME" /app

    exec gosu "$USERNAME" "$0" "$@"
fi

#
# --- Branding ---
#
cat << "EOF"
  _______          ___  _   __  __             _           _
 |_   _\ \        / / || | |  \/  |   /\      | |         (_)
   | |  \ \  /\  / /| || |_| \  / |  /  \   __| |_ __ ___  _ _ __
   | |   \ \/  \/ / |__   _| |\/| | / /\ \ / _` | '_ ` _ \| | '_ \
  _| |_   \  /\  /     | | | |  | |/ ____ \ (_| | | | | | | | | | |
 |_____|   \/  \/      |_| |_|  |_/_/    \_\__,_|_| |_| |_|_|_| |_|

EOF

echo
echo "Brought to you by RaidMax"
echo "-------------------------"
echo "UID: ${PUID:-0} / GID: ${PGID:-0}"
echo "-------------------------"
echo

#
# --- File & Directory Checks ---
#
CONFIG_DIR="/app/_Configuration"
DEFAULT_CONFIG_DIR="/app_defaults/_Configuration"

# Create config directory if it doesn't exist
mkdir -p "$CONFIG_DIR"

# Copy default config if not present
if [ ! -f "$CONFIG_DIR/Configuration.json" ]; then
    echo "Configuration.json not found, creating a default one."
    cp "$DEFAULT_CONFIG_DIR/Configuration.json" "$CONFIG_DIR/Configuration.json"
fi

#
# --- Start Application ---
#
echo "Configuration verified. Starting PlexWatch..."
exec dotnet PlexWatch.dll
