# Deploying Animal Database to a Hetzner VPS

One Docker Compose stack runs the whole site on a single small server:

| Service          | Image                               | Job                                                                 |
|------------------|-------------------------------------|---------------------------------------------------------------------|
| `caddy`          | `caddy:2`                           | Public entry point on ports 80/443, automatic HTTPS (Let's Encrypt) |
| `api`            | built from `deploy/Dockerfile`      | ASP.NET Core API plus the Angular site from `wwwroot`, port 8080    |
| `libretranslate` | `libretranslate/libretranslate:v1.9.6` | Optional DeepL fallback (en to de only); Docker network only, no host port |
| `mariadb`        | `mariadb:11.8`                      | Database, only reachable from the `api` and `backup` containers     |
| `backup`         | `mariadb:11.8`                      | Nightly `mariadb-dump`, gzipped, kept for 14 days                   |

The browser only ever talks to one origin (`https://<your domain>`): Angular calls `/api/...`, which is the
same server, so no CORS is needed in production. `/api/*` and `/swagger/*` are never answered with the
site's `index.html`; unknown API paths return a 404 JSON problem response. Swagger is only available when
`ASPNETCORE_ENVIRONMENT=Development`, so it is off on the server.

## Files

```
deploy/
  Dockerfile               multi-stage build: Node 22 (Angular), .NET 10 SDK (API), aspnet:10.0 runtime, non-root
  Dockerfile.dockerignore  keeps bin/obj/tests/.git out of the build context
  docker-compose.yml       caddy + api + libretranslate + mariadb + backup, named volumes
  Caddyfile                site address comes from DOMAIN in .env
  .env.example             copy to .env and fill in (placeholders only, .env is git-ignored)
  schema.sql               schema + data of the local animaldatabase (no users, no grants)
  init/02-app-user.sh      first start only: creates the limited app user from .env
  backup/backup.sh         nightly dump loop used by the backup service
  redeploy.ps1             PC -> server deploy without GitHub access on the server (see "Deploying from the PC")
  pull-backups.ps1         copies new server backups to D:\AnimalDatabase-server-backups (see "Backups and restore")
  watchdog/                health check every 5 minutes that posts to a webhook on down/recovered (see "Watchdog")
```

### How the Angular source gets into the image

The site lives in a separate repository, so the build uses a Docker **named build context** called
`ui-src`. `docker-compose.yml` points it at `UI_SOURCE` (default `../../AnimalDatabase_UI`, i.e. the UI
repo cloned next to this one). The Dockerfile copies only `package.json`, `package-lock.json`,
`angular.json`, the `tsconfig*.json` files, `src/` and `public/` from it, runs `npm ci` and
`ng build --configuration production`, and puts `dist/AnimalDatabaseProject/browser` into `/app/wwwroot`.

Expected layout on the server:

```
~/apps/AnimalDatabase_Server   (this repo; run compose from its deploy/ folder)
~/apps/AnimalDatabase_UI       (the Angular repo)
```

On a Windows PC with both repos on `D:` set `UI_SOURCE=D:/AnimalDatabaseProject` instead.

### ARM64 and amd64

The Node and .NET SDK stages run on the build machine's own platform (`--platform=$BUILDPLATFORM`) and
the API is published for `$TARGETARCH`, so the same Dockerfile builds natively on a Hetzner **CAX** (ARM64)
server and on x86 machines. To build an ARM64 image on an x86 PC:
`docker buildx build --platform linux/arm64 -f deploy/Dockerfile --build-context ui-src=<ui folder> .`

### Settings the API reads

| Setting (env var form)                | Default                                     | Meaning                                                     |
|---------------------------------------|---------------------------------------------|-------------------------------------------------------------|
| `ConnectionStrings__AnimalDatabase`   | (none)                                      | Built by compose from `.env`                                |
| `Admin__ApiKey`                       | (none)                                      | `ADMIN_API_KEY` from `.env`; without it editing is disabled |
| `Admin__TesterKey`                    | (none)                                      | Optional `TESTER_API_KEY` from `.env`; blank disables tester access |
| `DeepL__ApiKey`                       | (none)                                      | `DEEPL_API_KEY` from `.env`; Translate button needs it (primary) |
| `LibreTranslate__BaseUrl`             | `http://libretranslate:5000/`               | DeepL fallback; compose default points at the internal service. Set `LIBRETRANSLATE_BASE_URL=` empty to disable |
| `LibreTranslate__ApiKey`              | (none)                                      | Unused unless you enable `LT_API_KEYS` on the LibreTranslate container (`LIBRETRANSLATE_API_KEY`) |
| `Spa__Enabled` / `Spa__RootPath`      | `true` / `wwwroot`                          | Serve the Angular build if `index.html` exists there        |
| `Cors__AllowedOrigins__0`, `__1`, ... | none (Development: `http://localhost:4200`) | Extra origins allowed to call the API                       |
| `ForwardedHeaders__KnownProxies__0`   | none (loopback is always trusted)           | Proxy IPs whose `X-Forwarded-For` is trusted                |
| `ForwardedHeaders__KnownNetworks__0`  | none                                        | Same, as CIDR ranges                                        |
| `RateLimiting__*`                     | see appsettings                             | Per-IP limits                                               |

Caddy gets the fixed address `172.30.0.10` (`PROXY_IP`) on the `web` network, and that is the only
address the API trusts for `X-Forwarded-For`. Caddy replaces any `X-Forwarded-For` sent by the visitor,
so the rate limiter sees the real visitor IP and cannot be fooled by a forged header.

DeepL stays the primary translator. LibreTranslate is a fallback only: the `libretranslate` service listens on the Docker `web` network (no host port), loads only `en` and `de`, and is capped at about 768 MiB RAM. Leave `LIBRETRANSLATE_BASE_URL` unset to use the compose default, set it empty to turn the fallback off, or remove the service from compose if you do not want the container at all.
Cache headers for the site: `index.html` is `no-cache`; hashed Angular files (`main-XXXXXXXX.js`,
`styles-XXXXXXXX.css`, `chunk-...`) are `public, max-age=31536000, immutable`; anything else gets one hour.

## Server setup (Ubuntu 24.04, Hetzner Cloud)

### 1. Create the server

In the Hetzner Cloud console: **Add server**

- Location: Falkenstein, Nuremberg or Helsinki (EU)
- Image: **Ubuntu 24.04**
- Type: **Shared vCPU, Arm64 (Ampere), CAX11** (2 vCPU, 4 GB RAM, 40 GB disk)
- Networking: public IPv4 and IPv6
- SSH keys: add your public key (see below). Do not use a root password.
- Optional: enable Backups (+20%) for whole-server snapshots

Create an SSH key on your PC first if you do not have one (PowerShell):

```powershell
ssh-keygen -t ed25519 -C "hetzner-animaldatabase"
Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub
```

Paste the printed line into Hetzner's **SSH keys** box. Note the server's IPv4 address, e.g. `203.0.113.10`.

### 2. First login, a normal user, SSH keys only

```bash
ssh root@203.0.113.10

apt update && apt -y full-upgrade
adduser --disabled-password --gecos "" deploy
usermod -aG sudo deploy
echo "deploy ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deploy
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy

cat > /etc/ssh/sshd_config.d/10-hardening.conf <<'CONF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
CONF
systemctl restart ssh
```

Open a **second** terminal and check `ssh deploy@203.0.113.10` works before closing the root session.

### 3. Firewall: only 22, 80 and 443

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 443/udp
sudo ufw --force enable
sudo ufw status verbose
```

Docker publishes ports past ufw, which is why only Caddy publishes any; MariaDB and the API have no
published ports. Optionally also add a Hetzner Cloud Firewall with the same rules (Firewalls -> Create).

Automatic security updates:
`sudo apt -y install unattended-upgrades && sudo dpkg-reconfigure -plow unattended-upgrades`,
then allow reboots for kernel updates at a quiet hour (server clock is UTC):

```bash
printf '%s\n' 'Unattended-Upgrade::Automatic-Reboot "true";' 'Unattended-Upgrade::Automatic-Reboot-WithUsers "true";' \
  'Unattended-Upgrade::Automatic-Reboot-Time "03:30";' | sudo tee /etc/apt/apt.conf.d/52unattended-upgrades-reboot
sudo unattended-upgrade --dry-run --debug
```

Only the Ubuntu origins in `50unattended-upgrades` (release, `-security`, ESM security) are upgraded; Docker's own
repository is not in that list, so docker is never restarted by it (update it by hand). After a reboot every
compose service comes back (`restart: unless-stopped`, docker enabled at boot), and so does the watchdog timer.

### 4. Install Docker

```bash
sudo apt -y install ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker deploy
```

Log out and back in, then `docker run --rm hello-world`.

### 5. Clone both repositories

Both repositories are private, so give the server read-only **deploy keys** (one per repository; GitHub
does not allow the same deploy key on two repositories):

```bash
ssh-keygen -t ed25519 -N "" -f ~/.ssh/adb_server -C "server deploy key"
ssh-keygen -t ed25519 -N "" -f ~/.ssh/adb_ui -C "ui deploy key"
cat >> ~/.ssh/config <<'CONF'
Host github-adb-server
  HostName github.com
  User git
  IdentityFile ~/.ssh/adb_server
  IdentitiesOnly yes
Host github-adb-ui
  HostName github.com
  User git
  IdentityFile ~/.ssh/adb_ui
  IdentitiesOnly yes
CONF
cat ~/.ssh/adb_server.pub
cat ~/.ssh/adb_ui.pub
```

On GitHub add each public key under the matching repository -> Settings -> Deploy keys -> Add deploy key (leave "Allow
write access" off). Then, with `<github-user>` replaced by the account name:

```bash
mkdir -p ~/apps && cd ~/apps
git clone git@github-adb-server:<github-user>/AnimalDatabase_Server.git
git clone git@github-adb-ui:<github-user>/AnimalDatabase_UI.git
```

### 6. Fill in .env

```bash
cd ~/apps/AnimalDatabase_Server/deploy
cp .env.example .env
chmod 600 .env
openssl rand -hex 24
nano .env
```

Use a fresh `openssl rand -hex 24` value for `MARIADB_ROOT_PASSWORD`, `APP_DB_PASSWORD` and
`ADMIN_API_KEY` (hex only: no quotes, `$`, `;` or backslashes). Copy the admin key into your password
manager; it is what you type on the site's admin page.

`DOMAIN`:

- No domain: use `<ip-with-dashes>.sslip.io`, e.g. `203-0-113-10.sslip.io` for `203.0.113.10`.
  sslip.io is a free DNS service that resolves that name to the IP inside it, so Caddy can get a real
  Let's Encrypt certificate for it with no DNS setup.
- Own domain: create an `A` record (and `AAAA` for the IPv6) pointing at the server first, then put the
  name here, e.g. `animals.example.org`.

`OLD_DOMAIN` (optional): a previous address that should permanently redirect (308) to `DOMAIN`, e.g.
the sslip.io name after switching to your own domain. Leave it empty if there is none.

### 7. Start it

```bash
cd ~/apps/AnimalDatabase_Server/deploy
docker compose up -d --build
docker compose ps
docker compose logs -f api caddy
```

The first build takes a few minutes (npm ci, dotnet restore). On the first start MariaDB loads
`schema.sql` and runs `init/02-app-user.sh`; both only run while the `mariadb_data` volume is empty.

### 8. Check it

```bash
curl -sI https://$DOMAIN/ | head -n 5
curl -s https://$DOMAIN/api/animals | head -c 200; echo
curl -s -o /dev/null -w "%{http_code} %{content_type}\n" https://$DOMAIN/api/does-not-exist
curl -s -o /dev/null -w "%{http_code}\n" https://$DOMAIN/swagger
curl -s -o /dev/null -w "%{http_code}\n" -H "X-Admin-Key: <your key>" https://$DOMAIN/api/admin/check
```

(`DOMAIN` here is the value from `.env`; run `set -a; . ./.env; set +a` first.) Expected: `200`, a JSON
array, `404 application/problem+json`, `404`, `204`. Then open `https://<DOMAIN>/animals/1` in a browser
and reload it: deep links must load the site, not a 404.

## Updating

```bash
cd ~/apps/AnimalDatabase_UI && git pull
cd ~/apps/AnimalDatabase_Server && git pull
cd deploy && docker compose up -d --build
docker image prune -f
```

### Deploying from the PC (current setup)

The live server (https://unusualanimals.duckdns.org, `deploy@49.12.226.169`, `/opt/animaldatabase`; the old
https://49-12-226-169.sslip.io address redirects there via `OLD_DOMAIN`) has no GitHub access. The code is copied
from the PC instead, as `git archive` of `master` of both repos:

```
/opt/animaldatabase/
  AnimalDatabase_Server/   API repo, deploy/.env lives here (mode 600, never in git)
  AnimalDatabase_UI/       UI repo, picked up through UI_SOURCE=../../AnimalDatabase_UI
```

Commit (and push) first, then from PowerShell on the PC:

```powershell
D:\AnimalDatabase.Api\deploy\redeploy.ps1
```

It archives `master` of both repos, copies the archives over SSH, takes a backup, swaps the folders (keeping `.env`),
runs `docker compose up -d --build`, prunes old images and then checks that `/` and `/api/animals` answer 200 (it
retries for about a minute and fails loudly if they don't; it does not roll back). Only committed changes are
deployed. Options: `-Ref <branch>`, `-SkipBackup`, `-Server deploy@<ip>`, `-UiRepo <path>`.

`schema.sql` is not re-applied to an existing database. Schema changes need an `ALTER TABLE` run by hand (see "Database
shell" below).

### Before you deploy: database scripts

The API needs these scripts on the live database **before** the new code starts, or `/api/animals` fails. Run each one
once, in this order. Check first with `SHOW COLUMNS FROM animals; SHOW TABLES;` and skip what is already there:

| Script                                | What it does                                          |
|---------------------------------------|-------------------------------------------------------|
| `Database/habitat_type.sql`           | Adds `animals.habitat_type`                           |
| `Database/animal_translations.sql`    | Adds the `animal_translations` table                  |
| `Database/animal_translations_de.sql` | Loads the German texts                                |
| `Database/animal_units.sql`           | Converts animal measurements to canonical base units  |
| `Database/weight_range.sql`           | Ensures canonical weight columns use grams at 3 decimals |
| `Database/source_url.sql`             | Adds `animals.source_url`                             |
| `Database/animal_status.sql`           | Adds `animals.status`, `submitted_by`, `reviewed_at`, `review_note` |

Run `Database/animal_units.sql` locally first. Back up the live database and obtain approval before applying it live and deploying this API version.

Copy a script to the server and run it from the live `deploy` folder:

```powershell
scp D:\AnimalDatabase.Api\Database\source_url.sql deploy@49.12.226.169:/tmp/
```

```bash
cd /opt/animaldatabase/AnimalDatabase_Server/deploy
docker compose exec -T mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" "$MARIADB_DATABASE"' < /tmp/source_url.sql
rm /tmp/source_url.sql
```

### Logs and security headers

Every service keeps at most three 10 MB log files (`x-logging` in `docker-compose.yml`). The change applies when
containers are recreated, so the first deploy with it may restart Caddy, MariaDB and the backup service.

The Caddyfile sends an enforcing `Content-Security-Policy`. It allows no inline scripts, which is why `inlineCritical`
is turned off in the UI repo's `angular.json`. If it ever breaks the site, rename the header to
`Content-Security-Policy-Report-Only` so browsers only log violations.

`redeploy.ps1` doesn't recreate Caddy, so after changing the Caddyfile run this in the live `deploy` folder:
`docker compose up -d --no-deps --force-recreate caddy`.

## Backups and restore

The `backup` service writes `animaldatabase-YYYY-MM-DD_HHMMSS.sql.gz` to the host folder `BACKUP_HOST_DIR`
(default `../../backups` next to the two repos, on the live server `/opt/animaldatabase/backups`; the files
are readable by `deploy` without sudo, and the folder survives `redeploy.ps1`) every night at `BACKUP_TIME_UTC` (default
01:30 UTC) and deletes files older than `BACKUP_KEEP_DAYS`
(default 14).

```bash
docker compose exec backup bash /usr/local/bin/backup.sh now
docker compose exec backup ls -lh /backups
docker compose logs backup
```

Restore test (passed 2026-10-23): load the newest dump into a throwaway container with no network and a tmpfs
data dir, compare with live, then remove it. The live database is not touched.

```bash
PW=$(openssl rand -hex 16)
docker run -d --name adb-restore-test --network none --memory 300m --tmpfs /var/lib/mysql:rw,size=200m \
  -e MARIADB_ROOT_PASSWORD="$PW" mariadb:11.8 --innodb-buffer-pool-size=32M
F=$(ls -1t /opt/animaldatabase/backups/*.sql.gz | head -n1)
zcat "$F" | docker exec -i -e MYSQL_PWD="$PW" adb-restore-test mariadb -uroot
docker exec -e MYSQL_PWD="$PW" adb-restore-test mariadb -uroot animaldatabase \
  -e "SELECT COUNT(*) FROM animals; SELECT COUNT(*) FROM animal_images; SELECT COUNT(*) FROM searches"
docker rm -f -v adb-restore-test
```

Result on 2026-10-23: no errors; tables `animals`, `animal_images`, `searches` all present; 18 / 18 / 98 rows,
the same as live; Axolotl (11), Blobfish (22) and Greater blue-ringed octopus (27) match the live API, and an
MD5 over all `animals` and `animal_images` rows is identical to live.

This test is older than `animal_translations`, `habitat_type` and `source_url`. Repeat it after those reach the live
database, and add `SELECT COUNT(*) FROM animal_translations` to the check.

### Copies on the PC's SSD

`pull-backups.ps1` copies every server dump that isn't on the SSD yet into
`D:\AnimalDatabase-server-backups\`, checks each copy against the server's SHA-256, keeps the newest 60
files there (it never deletes anything on the server) and appends one line per run to `pull.log` in that
folder. If D: or the folder is missing, or the server can't be reached, it exits without doing anything.

```powershell
D:\AnimalDatabase.Api\deploy\pull-backups.ps1
```

The Windows Scheduled Task "AnimalDatabase pull backups" (current user, no admin) runs it hidden at logon
and daily at 18:00, and catches up after a missed run.

To restore one of those copies, copy it to the server and feed it to MariaDB:

```powershell
scp D:\AnimalDatabase-server-backups\animaldatabase-2026-10-24_013000.sql.gz deploy@49.12.226.169:/tmp/
ssh deploy@49.12.226.169
```

```bash
cd /opt/animaldatabase/AnimalDatabase_Server/deploy
docker compose stop api
gunzip -c /tmp/animaldatabase-2026-10-24_013000.sql.gz \
  | docker compose exec -T mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD"'
docker compose start api
rm /tmp/animaldatabase-2026-10-24_013000.sql.gz
```

To load one into the local MariaDB on the PC instead, unpack it (for example with 7-Zip) and run
`mariadb -u <user> -p < animaldatabase-....sql`; it replaces the local `animaldatabase`.

Restore a backup (replaces the database contents):

```bash
cd ~/apps/AnimalDatabase_Server/deploy
docker compose stop api
gunzip -c ../../backups/animaldatabase-2026-10-24_013000.sql.gz \
  | docker compose exec -T mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD"'
docker compose start api
```

The dumps contain `CREATE DATABASE`/`USE`, so they restore into the right database; the app user and
its grants are kept because they live in the `mysql` system database, which is not dumped.

Restore a dump taken on the PC (plain `.sql`, no `USE` line):

```bash
docker compose exec -T mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" "$MARIADB_DATABASE"' < schema.sql
```

Start completely fresh (deletes all server data, then reloads `schema.sql`):

```bash
docker compose down
docker volume rm animaldatabase_mariadb_data
docker compose up -d
```

## Watchdog

`watchdog/watchdog.sh` runs as root from a systemd timer every 5 minutes (`animaldatabase-watchdog.timer`).
It checks `https://<DOMAIN>/` and `/api/animals` through the local Caddy (`--resolve <DOMAIN>:443:127.0.0.1`)
and that the `caddy`, `api` and `mariadb` containers are running (and healthy where they have a
healthcheck). Two failed runs in a row count as down. It POSTs JSON (`status` = `down`, `recovered` or
`test`, `failed`, `detail`, `time` in UTC, `host`) only when the state changes, so at most one post per
outage and one on recovery; a failed post is retried on the next run. State: `/var/lib/animaldatabase-watchdog/state`.

The webhook URL and key live only in `/etc/animaldatabase-alert.env` (root, mode 600, format as in
`watchdog/alert.env.example`, never in git). The key is sent as `Authorization: Bearer <key>`.

Install or update on the server, from a folder holding the files:

```bash
sudo install -o root -g root -m 600 alert.env /etc/animaldatabase-alert.env
sudo install -o root -g root -m 755 watchdog.sh /usr/local/bin/animaldatabase-watchdog
sudo install -o root -g root -m 644 animaldatabase-watchdog.service animaldatabase-watchdog.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now animaldatabase-watchdog.timer
sudo animaldatabase-watchdog --test "watchdog installed"
```

Check it: `systemctl list-timers animaldatabase-watchdog.timer` and
`journalctl -u animaldatabase-watchdog.service -n 20`.

## Database shell

```bash
docker compose exec mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" "$MARIADB_DATABASE"'
```

## Refreshing schema.sql from the PC

Don't reformat `schema.sql` or the files in `Database/`: formatters can change the text inside SQL values. The
repository's `.editorconfig` turns Rider's formatter off for `*.sql`. A fresh dump replaces the hand-added section at
the end of the current `schema.sql`, so your local database needs all the scripts above first.

Run in PowerShell on the PC (the password is read from the prompt into an environment variable and
never shown):

```powershell
$env:MYSQL_PWD = Read-Host "MariaDB password" -MaskInput
& "C:\Program Files\MariaDB 11.8\bin\mariadb-dump.exe" -h 127.0.0.1 -u <db user> `
  --single-transaction --routines --triggers --events --hex-blob --skip-dump-date `
  --default-character-set=utf8mb4 --result-file=D:\AnimalDatabase.Api\deploy\schema.sql animaldatabase
Remove-Item Env:MYSQL_PWD
```

## Running the stack locally

With Docker Desktop, create an env file outside the repo, for example `%TEMP%\adb-docker\.env`, with
`DOMAIN=:80`, `HTTP_PORT=18088`, `HTTPS_PORT=18443`, `UI_SOURCE=D:/AnimalDatabaseProject` and test
passwords, then:

```powershell
cd D:\AnimalDatabase.Api\deploy
docker compose -p adbtest --env-file $env:TEMP\adb-docker\.env up -d --build
start http://localhost:18088
docker compose -p adbtest --env-file $env:TEMP\adb-docker\.env down -v
```

`DOMAIN=:80` makes Caddy serve plain HTTP without a certificate. To try Swagger, run the image directly in
Development: `docker run --rm -p 18090:8080 -e ASPNETCORE_ENVIRONMENT=Development animaldatabase-api`
and open `http://localhost:18090/swagger` (API calls fail there because it has no database).
