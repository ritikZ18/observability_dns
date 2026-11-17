# Current Device Speed Test (first version)

## Purpose

The existing DNS/TLS/HTTP probe workers test websites from the worker's cloud network. **This feature is different**: a visitor clicks **Open Speed Test** on the dashboard, and their browser sends measurement traffic directly to the configured remote API server. The worker, scheduler, incidents, and existing domain checks remain unchanged.

- UI route: /speed-test
- React page: ui/src/pages/SpeedTest.tsx
- Browser test runner: ui/src/services/speedTest.ts
- Measurement controller: src/api/Controllers/SpeedTestController.cs

## Measurements and limitations

- 8 sequential no-content HTTP ping requests → median idle latency, mean absolute difference between successive samples for jitter
- 3 download sizes: 512 KiB, 2 MiB, 8 MiB → sampled HTTP throughput (90th percentile)
- 3 upload sizes: 256 KiB, 1 MiB, 4 MiB → sampled HTTP throughput (90th percentile)
- One concurrent ping per transfer; the final transfer ping for each direction is displayed as an approximate loaded-latency observation
- 0% packet loss is **not** assumed. Packet loss is displayed as unavailable; reliable packet loss requires a separate WebRTC/TURN or native UDP implementation.
- No speed history is saved. This avoids changes to PostgreSQL schema and privacy policies during the initial release.

These are HTTP transfer measurements, not ICMP pings, not WAN-router measurements, and not a replacement for calibrated multi-stream throughput benchmarks. Latency measurements can include browser, proxy and server processing overhead. Results are measured against **your hosted API** and may be constrained by its region, CPU, congestion, proxy, egress bandwidth, and cloud billing quotas.

**Cost:** A successful full test transfers about 15.75 MiB of payload, excluding protocol overhead (10.5 MiB download and 5.25 MiB upload). Consider provider-level bandwidth quotas, abuse protections and separate measurement servers before inviting broad public usage. API endpoints have input caps and per-IP in-memory fixed-window rate limiting.

## Docker compose / local development

The Compose Vite frontend serves localhost:3002, and the API serves localhost:5000. A test that downloads from localhost is a **local loopback benchmark** and says nothing useful about Internet speed.

Compose explicitly sets `VITE_SPEED_TEST_BASE_URL=/api/speed-test`, enabling local browser-to-API testing through the existing Vite proxy. The page labels this as `BROWSER → LOCAL API`, and the smoke suite verifies local ping, download, upload, and UI routing. Outside Compose, the browser runner still refuses to run on localhost without an explicitly configured endpoint, preventing accidental interpretation as Internet bandwidth.

The Compose-only `VITE_API_PROXY_TARGET=http://api:8080` uses Docker service discovery. Do not use that hostname in browser-facing configuration: browsers continue to use localhost locally, while the production nginx image proxies `/api` to the API service. Deployment UI origins can be listed under `Cors:AllowedOrigins` or appended at runtime with a comma-separated `CORS_ALLOWED_ORIGINS` environment variable, for example `https://app.example.com,https://staging.example.com`.

To measure against a remote, publicly reachable HTTPS origin instead, set in ui/.env.local (create this file locally; do not commit secrets):

    VITE_SPEED_TEST_BASE_URL=https://YOUR-REMOTE-API.example.com/api/speed-test

Start Vite with npm run dev, or docker compose up --build ui.

For Compose development, override the local default when starting the stack if desired:

    VITE_SPEED_TEST_BASE_URL=https://YOUR-REMOTE-API.example.com/api/speed-test ./start.sh

In Vite production builds, the variable must be set at **build time**.

## Fly.io deployment (separate apps)

The repository uses distinct Fly apps (observability-dns-ui and observability-dns-api). Do not assume the UI container can reach the API at Docker Compose's internal hostname "api"; that hostname resolves only when both services share the compose network.

1. Deploy the updated API. Check GET https://YOUR-API.example.com/api/speed-test/ping returns HTTP 204.
2. Allow the UI origin in the ASP.NET Core Cors:AllowedOrigins configuration. The repository includes the named observability-dns-ui.fly.dev origin; change/add your custom domain if necessary.
3. Build/deploy the frontend with its remote API URL:

       fly deploy --config fly.ui.toml --app observability-dns-ui \
         --build-arg VITE_SPEED_TEST_BASE_URL=https://observability-dns-api.fly.dev/api/speed-test

4. Open the UI, click Open Speed Test, and inspect network requests in browser developer tools. Calls should go to the configured remote URL, not localhost.
5. Check server limits and monitor costs before enabling broad public access.

For a same-domain proxy with a working /api path, no explicit URL is needed when the UI is hosted remotely.

## API contract

    GET  /api/speed-test/ping
    GET  /api/speed-test/download?bytes=524288
    POST /api/speed-test/upload
         Content-Type: application/octet-stream

Downloads stream incompressible binary data; uploads consume and discard bytes and return {"bytesReceived":N}. All measurement endpoints are no-store, no-transform. Max download: 16 MiB per request. Max upload: 4 MiB per request. Rate limiting: 30 requests per client IP per minute, **per API instance**.

## Next iterations

- Proper multi-stream transfer testing with documented sample distributions
- Dedicated regional high-bandwidth test endpoints independent of normal app API
- Validated packet loss via WebRTC/TURN
- Optional persisted history in PostgreSQL with explicit consent and privacy policy
- Compare client-browser vs local gateway (requires a router or local agent)
- Automated regression tests for cancellation, request-size validation, and rate limits
