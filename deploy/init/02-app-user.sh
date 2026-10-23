#!/bin/bash
set -euo pipefail

: "${APP_DB_USER:?APP_DB_USER is not set}"
: "${APP_DB_PASSWORD:?APP_DB_PASSWORD is not set}"
: "${MARIADB_DATABASE:?MARIADB_DATABASE is not set}"

case "$APP_DB_USER$APP_DB_PASSWORD$MARIADB_DATABASE" in
  *"'"*|*'\'*|*'`'*)
    echo "APP_DB_USER, APP_DB_PASSWORD and MARIADB_DATABASE must not contain quotes, backticks or backslashes" >&2
    exit 1
    ;;
esac

MYSQL_PWD="${MARIADB_ROOT_PASSWORD}" mariadb --protocol=socket -uroot <<SQL
CREATE USER IF NOT EXISTS '${APP_DB_USER}'@'%' IDENTIFIED BY '${APP_DB_PASSWORD}';
GRANT SELECT, INSERT, UPDATE, DELETE ON \`${MARIADB_DATABASE}\`.* TO '${APP_DB_USER}'@'%';
FLUSH PRIVILEGES;
SQL

echo "Created database user ${APP_DB_USER} with read/write access to ${MARIADB_DATABASE}"
