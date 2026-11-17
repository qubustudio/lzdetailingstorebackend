# lzdetailingstorebackend
LZDetailingStore

## Deployment behind Cloudflare or a reverse proxy

The API reads the buyer's real IP (stored on each order) from `X-Forwarded-For`, but only when the
request comes from a trusted proxy. Configure the trusted proxies in the `ForwardedHeaders` section
(environment variables: `ForwardedHeaders__KnownNetworks__0`, `ForwardedHeaders__KnownNetworks__1`, ...):

- `KnownNetworks`: CIDR ranges. Behind Cloudflare, list **every** Cloudflare range from
  https://www.cloudflare.com/ips/ (IPv4 and IPv6) and keep the list up to date.
- `KnownProxies`: single IPs, e.g. a reverse proxy (nginx, Caddy) in front of the API.
- `ForwardLimit` (optional, default 1): set it to 2 when a reverse proxy sits behind Cloudflare.

With nothing configured only loopback is trusted, so behind Cloudflare every order would store the
Cloudflare edge IP. Never trust all proxies: anyone could then spoof their IP with the header.

The IP and User-Agent are cleared from orders older than 2 years by a daily background job, and are
only visible in the admin order detail with the `ordenes.seguridad` permission.
