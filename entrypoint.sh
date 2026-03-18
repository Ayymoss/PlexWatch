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
    else
        addgroup --gid "$GROUP_ID" "$GROUPNAME"
    fi

    USERNAME="appuser"
    if getent passwd "$USER_ID" >/dev/null; then
        USERNAME=$(getent passwd "$USER_ID" | cut -d: -f1)
    else
        adduser --system --uid "$USER_ID" --gid "$GROUP_ID" --shell /sbin/nologin "$USERNAME"
    fi

    chown -R "$USERNAME":"$GROUPNAME" /app
    exec gosu "$USERNAME" "$0" "$@"
fi

#
# --- Start Application ---
#
cat << "EOF"
 ____  _          __        __    _       _
|  _ \| | _____  _\ \      / /_ _| |_ ___| |__
| |_) | |/ _ \ \/ /\ \ /\ / / _` | __/ __| '_ \
|  __/| |  __/>  <  \ V  V / (_| | || (__| | | |
|_|   |_|\___/_/\_\  \_/\_/ \__,_|\__\___|_| |_|

EOF

echo "Starting PlexWatch..."
exec dotnet PlexWatch.dll
