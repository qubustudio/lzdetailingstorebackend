# Mercado Pago + Webhook Setup Guide

This guide documents how to configure Mercado Pago (Checkout Pro) payments for the
LZ Detailing Store backend, including local webhook testing with ngrok.

> **Audience:** developers setting up the payment flow locally or preparing production.
> **Payment model:** Checkout Pro (redirect flow). The backend creates a payment
> preference, redirects the customer to Mercado Pago, and receives payment
> confirmation via a signed webhook (IPN).

---

## Table of Contents

1. [Overview of the payment flow](#1-overview-of-the-payment-flow)
2. [Required credentials](#2-required-credentials)
3. [Mercado Pago application setup](#3-mercado-pago-application-setup)
4. [Storing secrets (User Secrets)](#4-storing-secrets-user-secrets)
5. [Webhook configuration](#5-webhook-configuration)
6. [Local testing with ngrok](#6-local-testing-with-ngrok)
7. [End-to-end test with sandbox test accounts](#7-end-to-end-test-with-sandbox-test-accounts)
8. [Troubleshooting](#8-troubleshooting)
9. [Going to production](#9-going-to-production)

---

## 1. Overview of the payment flow

```
Customer                Backend                 Mercado Pago
   |                       |                          |
   |-- POST /checkout ---->|                          |
   |                       |-- Create preference ---->|
   |                       |<-- preferenceUrl --------|
   |<-- preferenceUrl -----|                          |
   |                                                  |
   |-- Opens preferenceUrl (pays) ------------------->|
   |                                                  |
   |                       |<-- Webhook (signed) -----|  (payment approved)
   |                       |    validate signature    |
   |                       |    order -> Pagada        |
   |                       |    decrement stock        |
   |                       |    clear cart             |
   |                       |-- 200 OK --------------->|
```

Key backend files:

| Concern | File |
|---------|------|
| Create preference, validate signature, query payment | `src/LzDetailingStore.Infrastructure/Services/MercadoPagoService.cs` |
| Checkout command (build order + preference) | `src/LzDetailingStore.Application/Features/Commerce/Commands/CreateCheckout/CreateCheckoutCommandHandler.cs` |
| Webhook endpoint (reads query + body) | `src/LzDetailingStore.Api/Endpoints/Commerce/WebhookEndpoints.cs` |
| Webhook handler (status transition, stock) | `src/LzDetailingStore.Application/Features/Commerce/Commands/ProcessWebhook/ProcessWebhookCommandHandler.cs` |

---

## 2. Required credentials

The backend reads two Mercado Pago secrets from configuration:

| Config key | What it is | Where it comes from |
|------------|-----------|---------------------|
| `MercadoPago:AccessToken` | Authenticates API calls (create preference, query payment) | MP application → Test/Production credentials |
| `MercadoPago:WebhookSecret` | Validates the `x-signature` header on incoming webhooks | MP application → Webhooks → Secret key |

Optional development-only flag:

| Config key | Default | Purpose |
|------------|---------|---------|
| `MercadoPago:SkipSignatureValidation` | `false` | When `true`, the webhook accepts requests without a valid signature. **Local development only.** Never enable in staging/production. |

The `App:FrontendUrl` value is used to build the `back_urls` (success/failure/pending).

---

## 3. Mercado Pago application setup

The application **must** be configured as **Checkout Pro / "Otras plataformas"**.

> **Common mistake:** creating the app as a "WooCommerce" (or other e-commerce
> platform) integration. That token cannot create preferences via the API and
> returns `403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES`.

Steps in the [Mercado Pago developer panel](https://www.mercadopago.com.mx/developers/panel/app):

1. **Crear aplicación** (or edit the existing one).
2. **Elige una solución para integrar** → **Checkout Pro**.
3. **Plataforma** → **Otras plataformas**.
4. **Tipo de API** → **API de Preferences**.
5. Create the application.
6. Go to **Credenciales de prueba** and copy the **Access Token**.

The application detail should read:

- **Integración con:** Otras plataformas
- **Modelo de integración:** CheckoutPro

---

## 4. Storing secrets (User Secrets)

Secrets are stored with .NET User Secrets in development (never committed to git).
Run these from any terminal, pointing at the API project:

```powershell
# Initialize once (adds a UserSecretsId to the .csproj)
dotnet user-secrets init --project "src/LzDetailingStore.Api/LzDetailingStore.Api.csproj"

# Set / update the access token (set overwrites if it already exists)
dotnet user-secrets set "MercadoPago:AccessToken" "TEST-xxxxxxxx" --project "src/LzDetailingStore.Api/LzDetailingStore.Api.csproj"

# Set / update the webhook secret
dotnet user-secrets set "MercadoPago:WebhookSecret" "your-secret" --project "src/LzDetailingStore.Api/LzDetailingStore.Api.csproj"

# List configured keys
dotnet user-secrets list --project "src/LzDetailingStore.Api/LzDetailingStore.Api.csproj"
```

> **Important:** the named `MercadoPago` HttpClient reads the access token **at
> application startup**. After changing the token, **restart the backend** for it
> to take effect.

---

## 5. Webhook configuration

In the Mercado Pago panel, under the application → **Webhooks** →
**Configurar notificaciones**:

1. Select the **Modo de prueba** tab (for testing) or **Modo productivo** (for production).
2. **URL** → your public webhook endpoint:
   ```
   https://<your-public-host>/api/v1/webhooks/mercadopago
   ```
   For local testing this is the ngrok URL (see next section).
3. **Eventos** → check **Pagos (legacy)** only. Uncheck everything else.
   > Despite the "legacy" label, this is the event that sends the `payment` topic
   > with the `x-signature` header the backend validates.
4. **Guardar** → Mercado Pago generates the **Clave secreta** (webhook secret).
5. Copy that secret into `MercadoPago:WebhookSecret` (see previous section) and
   restart the backend.

> The webhook secret belongs to the **same application** that creates the
> preference. If you create preferences with app A's access token, the webhook
> notifications are signed with app A's secret.

---

## 6. Local testing with ngrok

Mercado Pago must reach your local backend over the public internet. ngrok creates
a public HTTPS tunnel to `localhost`.

### Install / update ngrok

```powershell
# Install (Windows, winget)
winget install --id ngrok.ngrok

# IMPORTANT: MP accounts require ngrok agent >= 3.20. The winget package may be
# older, so update it:
ngrok update
ngrok version   # confirm >= 3.20
```

### Configure your authtoken (one time)

Create a free account at <https://dashboard.ngrok.com/signup>, copy your authtoken
from **Getting Started -> Your Authtoken**, then:

```powershell
ngrok config add-authtoken <YOUR_AUTHTOKEN>
```

### Start the tunnel

The backend runs on `http://localhost:5297` (http launch profile). Point ngrok at
that port:

```powershell
ngrok http 5297
```

ngrok prints a public URL like:

```
https://xxxx-xxxx.ngrok-free.app
```

Your webhook URL for Mercado Pago is that host + the endpoint path:

```
https://xxxx-xxxx.ngrok-free.app/api/v1/webhooks/mercadopago
```

Useful local endpoints while ngrok runs:

- `http://localhost:4040` — ngrok web inspector (see requests in real time)
- `http://localhost:4040/api/tunnels` — JSON with the current public URL

> **Free plan caveat:** the public URL **changes every time you restart ngrok**.
> When it changes, update the webhook URL in the Mercado Pago panel.

---

## 7. End-to-end test with sandbox test accounts

Sandbox payments require **two distinct test accounts**: a **seller** and a
**buyer**. You cannot pay yourself with your real account (the "Pagar" button stays
disabled, or MP shows *"Una de las partes con la que intentas hacer el pago es de prueba"*).

### Create test accounts

Panel → **Cuentas de prueba** → **Crear cuenta de prueba**:

- One **Vendedor** (seller) account.
- One **Comprador** (buyer) account — gets fake balance (e.g. $200,000).

Each account has a **Usuario** (`TESTUSER...`), **Contraseña**, and
**Código de verificación**.

### Use the seller account's credentials

The access token used by the backend **must belong to the seller test account**,
not your personal account. Log in to the panel **as the seller test account**,
create a Checkout Pro application there, and use its test Access Token and
Webhook Secret.

### Run the test

1. Backend running (`http://localhost:5297`) with the seller test account's
   Access Token + Webhook Secret in User Secrets.
2. ngrok running; webhook URL registered in the seller account's app.
3. Create a preference via `POST /api/v1/checkout` (authenticated; cart must have
   items and the user must have a default shipping address).
4. Open the returned `preferenceUrl` in an **InPrivate/incognito window** logged
   in as the **buyer test account** (use the `TESTUSER...` username, password, and
   verification code from the panel — MP does not send a real email).
5. Pay with **"Dinero en Mercado Pago"** (fake balance) or a test card with
   cardholder name **APRO** (forces approval).

### Trigger the webhook reliably

The automatic webhook after a sandbox payment is unreliable. Use Mercado Pago's
**official simulator** instead:

Panel → application → **Webhooks** → **Simular**:

- **Tipo de evento:** Payment / Pagos
- **Data ID:** the real payment/operation id (shown on the "pago aprobado" screen)
- **Enviar** → MP sends a **correctly signed** notification to your ngrok URL.

Expected result: `Respuesta: 200 - OK`.

### Verify the outcome

Check the database (or the order endpoints):

- Order `status` → `Pagada`
- `mercado_pago_payment_id` populated
- Variant `stock` decremented by the purchased quantity
- The user's cart items removed

---

## 8. Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `403 PA_UNAUTHORIZED_RESULT_FROM_POLICIES` on create preference | App configured as WooCommerce / wrong product | Reconfigure app as **Otras plataformas + Checkout Pro** |
| `400 invalid_auto_return "back_url.success must be defined"` | `auto_return` sent with a `localhost` back URL | The service only sends `auto_return` when the success URL is public https. Use a public `App:FrontendUrl` in prod. |
| Webhook returns `401 INVALID_SIGNATURE` | Missing/invalid `x-signature`, or wrong/empty `WebhookSecret` | Ensure the `WebhookSecret` matches the app that created the preference. This 401 is **correct behavior** for unsigned requests. |
| "Pagar" button disabled in checkout | Paying with your own/real account | Pay with a **buyer test account** (both parties must be test accounts) |
| MP: *"Una de las partes ... es de prueba"* | Seller token is your real account, buyer is a test account | Use the **seller test account's** Access Token for the preference |
| ngrok: `ERR_NGROK_121` agent too old | ngrok agent `< 3.20` | `ngrok update` |
| Token changes not taking effect | HttpClient reads token at startup | Restart the backend |
| Webhook never arrives automatically | Sandbox auto-notification is unreliable | Use the **Simular** button in the MP panel |

---

## 9. Going to production

The **code does not change** between test and production — only the secrets and URLs:

1. Complete the business account verification in Mercado Pago.
2. In the panel, use **Credenciales de producción** for the real business account.
3. Set the production `MercadoPago:AccessToken` and `MercadoPago:WebhookSecret`
   via environment variables (`MercadoPago__AccessToken`, `MercadoPago__WebhookSecret`)
   — do **not** commit them.
4. Register the production webhook URL (your real API domain over HTTPS):
   ```
   https://api.lzdetailing.com/api/v1/webhooks/mercadopago
   ```
5. Ensure `MercadoPago:SkipSignatureValidation` is **not set** (or `false`).
6. Set `App:FrontendUrl` to the real frontend domain so `back_urls` are public and
   `auto_return` works.
