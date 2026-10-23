#!/usr/bin/env bash
set -uo pipefail

ENV_FILE="${ALERT_ENV_FILE:-/etc/animaldatabase-alert.env}"
STATE_DIR="${WATCHDOG_STATE_DIR:-/var/lib/animaldatabase-watchdog}"
SITE_DOMAIN="${SITE_DOMAIN:-unusualanimals.duckdns.org}"
PROJECT="${COMPOSE_PROJECT:-animaldatabase}"
SERVICES=(caddy api mariadb)
FAILS_FOR_DOWN=2

if [[ ! -r "$ENV_FILE" ]]; then
  echo "watchdog: cannot read $ENV_FILE" >&2
  exit 2
fi
set -a
. "$ENV_FILE"
set +a
if [[ -z "${ALERT_URL:-}" || -z "${ALERT_KEY:-}" ]]; then
  echo "watchdog: ALERT_URL or ALERT_KEY missing in $ENV_FILE" >&2
  exit 2
fi

mkdir -p "$STATE_DIR"
chmod 700 "$STATE_DIR"
STATE_FILE="$STATE_DIR/state"

failed=()
details=()

check_url() {
  local name="$1" path="$2" out code rc
  out=$(curl -s -o /dev/null -w '%{http_code}' --max-time 15 \
    --resolve "$SITE_DOMAIN:443:127.0.0.1" "https://$SITE_DOMAIN$path" 2>/dev/null)
  rc=$?
  code="${out:-000}"
  if [[ $rc -ne 0 ]]; then
    failed+=("$name")
    details+=("$name: curl error $rc (HTTP $code)")
  elif [[ "$code" != 200 ]]; then
    failed+=("$name")
    details+=("$name: HTTP $code")
  fi
}

check_container() {
  local service="$1" id state health
  id=$(docker ps -aq --filter "label=com.docker.compose.project=$PROJECT" \
    --filter "label=com.docker.compose.service=$service" 2>/dev/null | head -n 1)
  if [[ -z "$id" ]]; then
    failed+=("container:$service")
    details+=("container $service: missing")
    return
  fi
  read -r state health < <(docker inspect -f '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$id" 2>/dev/null)
  if [[ "$state" != running || ( "$health" != healthy && "$health" != none ) ]]; then
    failed+=("container:$service")
    details+=("container $service: ${state:-unknown}/${health:-unknown}")
  fi
}

post() {
  local status="$1" detail="$2" components="$3" body code rc
  body=$(STATUS="$status" DETAIL="$detail" COMPONENTS="$components" HOST_NAME="$(hostname)" python3 -c '
import json, os, datetime
print(json.dumps({
    "status": os.environ["STATUS"],
    "site": "https://" + os.environ.get("SITE_DOMAIN", ""),
    "failed": [c for c in os.environ["COMPONENTS"].split(",") if c],
    "detail": os.environ["DETAIL"],
    "time": datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds"),
    "host": os.environ["HOST_NAME"],
}))')
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 20 \
    -K <(printf 'url = "%s"\nheader = "Authorization: Bearer %s"\nheader = "Content-Type: application/json"\n' "$ALERT_URL" "$ALERT_KEY") \
    --data-binary "$body" 2>/dev/null)
  rc=$?
  echo "watchdog: posted $status, HTTP ${code:-000}, curl exit $rc"
  [[ $rc -eq 0 && "$code" =~ ^2 ]]
}

export SITE_DOMAIN

if [[ "${1:-}" == "--test" ]]; then
  post test "${2:-watchdog test}" ""
  exit $?
fi

check_url site /
check_url api /api/animals
for s in "${SERVICES[@]}"; do check_container "$s"; done

status=up
fails=0
if [[ -f "$STATE_FILE" ]]; then
  read -r status fails < "$STATE_FILE" || true
fi
[[ "$fails" =~ ^[0-9]+$ ]] || fails=0

if [[ ${#failed[@]} -eq 0 ]]; then
  if [[ "$status" == down ]]; then
    if post recovered "all checks passed" ""; then status=up; fi
  else
    status=up
  fi
  fails=0
  echo "watchdog: healthy"
else
  fails=$((fails + 1))
  joined_detail=$(IFS=';'; echo "${details[*]}")
  joined_failed=$(IFS=,; echo "${failed[*]}")
  echo "watchdog: check failed ($fails in a row): $joined_detail"
  if [[ "$status" != down && $fails -ge $FAILS_FOR_DOWN ]]; then
    if post down "$joined_detail" "$joined_failed"; then status=down; fi
  fi
fi

printf '%s %s\n' "$status" "$fails" > "$STATE_FILE.tmp" && mv "$STATE_FILE.tmp" "$STATE_FILE"
exit 0
