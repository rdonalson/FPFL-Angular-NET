# UI-NG — Angular Frontend for FPFL-Angular-NET

UI-NG is the Angular 22 + TypeScript frontend for the FPFL platform.
It is built entirely from standalone components (no NgModules), runs zoneless with signals, and integrates cleanly with the .NET 10 API.

It replaces the original version's NgModule/NgRx front end, keeping the same product while adopting the architecture used in the sister [UI-TSX React app](https://github.com/rdonalson/FPFL-React-Java/tree/main/UI-TSX).

Live at <https://ui-ng.ledger-finance.com> · API at <https://api-net.ledger-finance.com>

> 🚧 **Work in progress.** UI-NG is being built up to full feature parity with UI-TSX. See [Project Status](#-project-status) for what's done and what's next.

---

## 🚀 Tech Stack

| Area | Choice |
| ---- | ------ |
| **Framework** | Angular 22 + TypeScript 6 — standalone components, **zoneless** change detection, `OnPush` by default |
| **Build tool** | Angular CLI with the esbuild‑based `@angular/build:application` builder |
| **UI components** | PrimeNG 22 · PrimeIcons 8 |
| **Theming** | `@primeuix/themes` **Aura** preset (dark mode ready via an `.app-dark` class) · SCSS with CSS custom properties |
| **State** | Angular **signals** (`signal`, `computed`, `effect`) — no NgRx store |
| **Server state** | **`httpResource`** for reads (loading / value / error as signals), `HttpClient` + RxJS for writes |
| **Routing** | Angular Router with lazy `loadComponent` routes and functional guards |
| **Forms** | Reactive Forms (`NonNullableFormBuilder`) with built‑in and custom validators |
| **HTTP** | `provideHttpClient(withFetch())` with functional interceptors |
| **Auth** | JWT access tokens + single‑use refresh tokens, silent refresh, expiry warning |
| **Testing** | Vitest + jsdom via `@angular/build:unit-test`, `HttpTestingController` |
| **Tooling** | Prettier · `.editorconfig` |

---

## 📁 Project Structure

The folder layout mirrors the backend's bounded contexts to keep the system consistent, modular, and easy to onboard.

```
UI-NG/
 ├── public/                     # Static assets copied as-is
 │    ├── images/                # avatar + galleria/ (18 landing-page photos)
 │    └── media/                 # financial-planner-guide.pdf
 ├── src/
 │    ├── app/
 │    │    ├── core/             # App-wide building blocks
 │    │    │    ├── api/         # ApiResponse<T> envelope, ApiError, apiUrl()
 │    │    │    ├── auth/        # AuthService, AuthUi, guards, models
 │    │    │    │    └── ui/     # login / register / change-password / session-expiring dialogs
 │    │    │    ├── http/        # loading, error and auth interceptors
 │    │    │    ├── layout/      # topbar, footer, sidebar
 │    │    │    └── system/      # not-found, error pages
 │    │    ├── features/         # Lazy-loaded pages, one folder per domain
 │    │    │    ├── home/        # Galleria landing page
 │    │    │    ├── item-types/  # list + admin add / edit / delete
 │    │    │    ├── time-periods/# list + admin add / edit / delete
 │    │    │    ├── item-detail/ # items + initial amount (scaffolded)
 │    │    │    ├── display/     # ledger + chart (scaffolded)
 │    │    │    ├── admin/       # status page (scaffolded)
 │    │    │    └── docs/        # docs page (scaffolded)
 │    │    ├── app.config.ts     # Providers (replaces AppModule)
 │    │    ├── app.routes.ts     # Top-level routes
 │    │    └── app.ts / .html    # Application shell
 │    ├── environments/          # environment.ts (prod) / environment.development.ts
 │    ├── testing/               # Shared test providers and helpers
 │    ├── main.ts                # bootstrapApplication(App, appConfig)
 │    └── styles.scss            # Global styles and design tokens
 ├── Dockerfile                  # Multi-stage build → unprivileged nginx
 ├── nginx.conf                  # SPA fallback, caching, /healthz
 ├── docker-compose.yml          # Local container run (API at localhost:5132)
 ├── docker-compose.prod.yml     # Image pointed at the public API
 ├── angular.json
 └── package.json
```

Each feature keeps its own `data-access/` service, model, component, template, styles, and spec side by side.

---

## ⚙️ Configuration

Angular environment files are swapped at build time by `fileReplacements` in [`angular.json`](angular.json):

| File | Used by | API URL |
| ---- | ------- | ------- |
| [`environment.development.ts`](src/environments/environment.development.ts) | `ng serve`, development builds | `http://localhost:5132` |
| [`environment.ts`](src/environments/environment.ts) | Production builds | `https://api-net.ledger-finance.com` |

| Key | Purpose |
| --- | ------- |
| `production` | Production flag |
| `apiUrl` | API base URL used by `apiUrl()` and to decide which requests get a bearer token |
| `primeuiLicense` | Free PrimeUI Community license key passed to `providePrimeNG({ license })` |

> ⚠️ Environment values are inlined **at build time**. Changing the API URL means rebuilding — the [`Dockerfile`](Dockerfile) takes an `API_URL` build arg and writes it into `environment.ts` before `ng build`, and the CI workflow passes the production URL. The PrimeUI license key ships in the bundle by design; it is not a secret.

---

## 🔌 HTTP Layer

All requests go through `HttpClient`, configured in [`app.config.ts`](src/app/app.config.ts) with three functional interceptors (outermost first):

1. [`loadingInterceptor`](src/app/core/http/loading-interceptor.ts) — drives the global progress bar through the [`LoadingService`](src/app/core/http/loading.ts) signal
2. [`errorInterceptor`](src/app/core/http/error-interceptor.ts) — turns every failure into an [`ApiError`](src/app/core/api/api-response.ts) `{ status, message, correlationId, url }`, reading the API's envelope when present, so components never handle a raw `HttpErrorResponse`
3. [`authInterceptor`](src/app/core/http/auth-interceptor.ts) — adds `Authorization: Bearer <accessToken>` to API URLs only (never to third parties), and on a 401 performs one shared token refresh and retries; if the refresh fails the session ends with an "expired" notice

Every API response is typed as [`ApiResponse<T>`](src/app/core/api/api-response.ts) — `{ status, message, data, correlationId, timestamp }`, the same envelope API‑NET and API‑JDK return. Error screens show the `correlationId` as a reference that matches the server log.

Features read data with **`httpResource`** inside a small `data-access` service (for example [`ItemTypesApi`](src/app/features/item-types/data-access/item-types-api.ts)), which replaces the original version's NgRx actions, effects, and reducers.

---

## 🔐 Authentication & Session

- [`AuthService`](src/app/core/auth/auth.ts) holds the session in a signal and exposes `isAuthenticated`, `isGuest`, `isAdmin`, `displayName`, and `accessToken` as computed signals
- Login, guest login, register, refresh, change password, and logout map one‑to‑one onto API‑NET's `/auth/*` endpoints; **guest login sends no password** — the server signs the guest in
- The session is kept in `sessionStorage` (per tab); on startup `provideAppInitializer` restores it and refreshes an access token that is about to expire
- Concurrent 401s share a **single in‑flight refresh**, and each refresh token is single‑use
- [`AuthUi`](src/app/core/auth/auth-ui.ts) coordinates the [login](src/app/core/auth/ui/login-dialog/login-dialog.ts), [register](src/app/core/auth/ui/register-dialog/register-dialog.ts), and [change‑password](src/app/core/auth/ui/change-password-dialog/change-password-dialog.ts) dialogs, return‑URL handling, and toasts
- [`SessionExpiringDialog`](src/app/core/auth/ui/session-expiring-dialog/session-expiring-dialog.ts) warns **two minutes** before the access token expires and offers **Stay logged in** (refresh) or **Logout**
- [`authGuard`](src/app/core/auth/auth-guard.ts) protects signed‑in routes — signed‑out visitors go home with the login dialog open and return to the requested page after signing in; [`adminGuard`](src/app/core/auth/admin-guard.ts) is ready for admin‑only pages
- Change Password is hidden for the guest account, and admin‑only actions (add / edit / delete) are shown only to `ROLE_ADMIN` — the API enforces the same rules, so the UI is a convenience rather than the only defense

---

## 🗺️ Routes

| Path | Access | Page |
| ---- | ------ | ---- |
| `/` | Public | Home — Galleria slideshow, overview, guide PDF, sign‑in options |
| `/item-types` | Authenticated | Item types list · **admin**: add, edit (name and id), delete |
| `/time-periods` | Authenticated | Time periods list · **admin**: add, edit (name and id), delete |
| `**` | Public | Not found |

Every route is lazy‑loaded with `loadComponent`. Items, initial amount, the forecasted ledger and chart, and the admin status page are scaffolded under `features/` and will be routed as their API endpoints are ported.

---

## 📦 Features

Each feature folder under `src/app/features` encapsulates:

- A `data-access/` service (`httpResource` reads, `HttpClient` writes)
- A model interface matching the API DTO
- A standalone `OnPush` component with its template, styles, and spec

Implemented today:

- **`home`** — PrimeNG Galleria auto‑cycling through landing photos, the project summary, the Financial Planner Guide PDF, and guest / sign‑in calls to action
- **`item-types`** and **`time-periods`** — PrimeNG tables with refresh, error messages with correlation reference, and an admin dialog for add / edit / delete whose name validation matches the API's strict sanitizer

---

## 🚧 Project Status

The goal is the same feature set as [UI-TSX](https://github.com/rdonalson/FPFL-React-Java/tree/main/UI-TSX).

| Area | Status |
| ---- | ------ |
| App shell — top bar, footer, progress bar, toasts | ✅ Done |
| Landing page — Galleria slideshow, guide PDF | ✅ Done |
| Login, guest login, register, change password, logout | ✅ Done |
| Silent token refresh, session‑expiring dialog | ✅ Done |
| Item types — list + admin add / edit / delete | ✅ Done |
| Time periods — list + admin add / edit / delete | ✅ Done |
| Docker image, AKS deployment, CI/CD | ✅ Done |
| Sidebar menu with role‑filtered admin section | 🔜 Planned |
| Credit / debit items with per‑period add / edit pages | 🔜 Planned |
| Initial amount | 🔜 Planned |
| Forecasted ledger table + chart with date‑range selection | 🔜 Planned |
| Admin status page (UI + API health) | 🔜 Planned |
| Client error forwarding to `/client-logs` | 🔜 Planned |

---

## 🧪 Running the App

```bash
npm install
npm start
```

The dev server runs on **<http://localhost:4200>** and calls the API at `http://localhost:5132`. Requires **Node 24.15+**.

### Scripts

| Script | Purpose |
| ------ | ------- |
| `npm start` | `ng serve` on port 4200 |
| `npm run build` | Production build to `dist/ui-ng/browser` |
| `npm run watch` | Development build in watch mode |
| `npm test` | Vitest unit tests (jsdom) |

---

## 🧪 Testing

Unit tests run on **Vitest** through Angular's `@angular/build:unit-test` builder. The suite covers the auth service, the auth and error interceptors, guards, the shell, layout, and feature pages, using the real interceptors against `HttpTestingController`.

Shared setup lives in [`src/testing/test-providers.ts`](src/testing/test-providers.ts) (`appTestProviders()`, `envelope()`, `authResponse()`).

```bash
npm test
```

---

## 🐳 Docker

[`Dockerfile`](Dockerfile) is a two‑stage build: `node:24-alpine` runs `npm ci`, writes the `API_URL` build arg into `environment.ts`, and runs `ng build --configuration production`; the runtime stage serves `dist/ui-ng/browser` from **`nginxinc/nginx-unprivileged`** on port **8080** as a non‑root user.

[`nginx.conf`](nginx.conf) adds SPA fallback (`try_files … /index.html`), one‑year caching for hashed bundles, `no-cache` for `index.html`, gzip, basic security headers, and a `/healthz` probe endpoint.

```bash
docker compose up --build                              # API at http://localhost:5132, UI at http://localhost:4280
docker compose -f docker-compose.prod.yml up --build   # pointed at https://api-net.ledger-finance.com
```

---

## 🔄 CI/CD

Pushes to `master` touching `UI-NG/**` or `k8s/ui-ng/**` trigger [`build-push-ui-ng.yml`](../.github/workflows/build-push-ui-ng.yml):

1. Build and push `fpflacr.azurecr.io/fpfluingimg` tagged `:${{ github.sha }}` and `:latest`, passing `API_URL` as a build arg
2. Azure login → set AKS context (`fpfl-cluster`) → `kubectl apply` the UI service, deployment, and ingress
3. `kubectl rollout restart deployment/ui-ng -n fpfl` and wait for `rollout status`

The pod runs on the dedicated `netpool` node pool behind the nginx ingress at `ui-ng.ledger-finance.com`. Full pipeline details are in the [root README](../README.md#-cicd-pipeline).

---

## 🧰 Development Notes

- No NgModules — every component, directive, and pipe is standalone and imports what it uses
- Prefer signals and `computed` over RxJS subjects for component state; use `httpResource` for reads
- Never call `HttpClient` from a component; go through the feature's `data-access` service so the interceptors and `ApiResponse<T>` typing apply
- Use PrimeNG template references (`#header`, `#body`, `#item`, `#emptymessage`) rather than `pTemplate`
- Keep component styles within the `anyComponentStyle` budget in `angular.json`; shared styles belong in `styles.scss`
- Never remove or hide PrimeNG's license check — the key belongs in `environment.ts`

---

## 📄 License

This project is licensed under the [MIT License](../LICENSE).
