# Verteco SmartJob

Person 1 (Alex)

This folder is the backend, API, and reporting starting point for the group project. It contains Domain, Application, Infrastructure, API, and Contracts projects so the web and mobile teams can share DTOs.

All jobs use one **Standard Job Card** and its six checklist items. Clients do not choose or edit a template.

## What's included

- Guarded job card lifecycle: Draft → Assigned → Accepted → InProgress → AwaitingApproval → Completed, with Reopened and Cancelled branches.
- Submission validation for required fields, evidence, and both signatures; SHA-256 revision snapshots linked across resubmissions.
- Customer, site, user, and technician management services, soft deletion, and audit records.
- Flagged jobs and persisted corrective actions with manager resolution and audit records.
- Azure SQL EF Core provider, seed data, migrations, and an idempotent SQL deployment script.
- Private Azure Blob Storage adapter, technician job routes, scoped evidence and signature uploads, and offline mobile sync. See [the mobile API guide](docs/mobile-api-testing.md) and its separate Postman collection.
- `/api/v1` manager endpoints, dashboard aggregates, monthly reports, optional Gemini commentary, CSV/PDF exports, Swagger, and a Postman collection.

## Open the solution

Install the .NET 10 SDK and open `VertecoSmartJob.slnx` in Visual Studio. To run the automated .NET tests from this folder:

```powershell
dotnet restore VertecoSmartJob.slnx
dotnet build VertecoSmartJob.slnx
dotnet test tests/VertecoSmartJob.Tests/VertecoSmartJob.Tests.csproj
```

The automated tests use an isolated in-memory SQLite database. The running API uses Azure SQL.

## Set up Azure for local development


### Save local settings in Visual Studio User Secrets

1. In Visual Studio **Solution Explorer**, right-click the **VertecoSmartJob.Api project** (not the solution) and choose **Manage User Secrets**. The project already has a `UserSecretsId`.
2. Merge the following JSON into the `secrets.json` file that opens. Replace every placeholder with your own value; keep any other secrets you already use.

   ```json
   {
     "ConnectionStrings": {
       "AzureSql": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;User ID=<sql-login>;Password=<sql-password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
     },
     "Blob": {
       "ConnectionString": "<full connection string copied from Storage account > Access keys>",
       "Container": "job-evidence"
     },
     "ApiKeys": {
       "Accounts": [
         {
           "Email": "manager@verteco.example",
           "Key": "<long unique development key>"
         }
       ]
     },
     "Jwt": {
       "SigningKey": "<a long random string, at least 32 characters>",
       "Issuer": "VertecoSmartJob",
       "Audience": "VertecoSmartJob.Clients"
     },
     "Smtp": {
       "Host": "smtp.gmail.com",
       "Port": 587,
       "Username": "<a Gmail address used only for this project>",
       "Password": "<a 16-character Gmail App Password, not your normal Gmail password>",
       "FromAddress": "<same Gmail address as Username>"
     }
   }
   ```

   The `Email` must match the seeded manager user. Choose a long random development key and use its **exact value** in Postman's `managerKey` collection variable. The key is for this local development authentication harness, not your Azure SQL password or Azure account password.

   `Jwt` and `Smtp` are Person 3's (Moe's) addition — see that section below for what they're for and how to generate them. Every developer running the Api locally needs their own values for all of these; none of them are committed to the repo.
3. Save `secrets.json`, set **VertecoSmartJob.Api** as the startup project, select its **http** launch profile, and press **F5**. The API uses `http://localhost:5196`; Swagger is at `http://localhost:5196/swagger`. The **https** profile also exposes `https://localhost:7086`. The launch profiles set `ASPNETCORE_ENVIRONMENT=Development`, which makes User Secrets available.

User Secrets live outside the repository and everyone configures their own local file. They are intended for development and are not encrypted. Never put real passwords, storage keys, or API keys in `appsettings.json`, the README, the Postman collection, or a commit.
Development startup applies pending Azure SQL EF migrations. The `AddStandardJobCardTemplate` migration adds only the single required template and its six checklist items when no active standard template exists. Startup does **not** insert accounts or demo records. The existing Manager account and password set through Person 3's login flow remain in the database; the API never recreates them. Automated tests use a separate in-memory SQLite database and test-only fixtures. The database must be reachable before the API can start. If the SQL firewall or credentials are wrong, startup stops during migration. The Api now authenticates requests through **either** the development API-key header **or** a real JWT Bearer token — see Person 3's section below for how real login, password reset, and role enforcement work. Production startup is still intentionally blocked (see `Program.cs`) until the team confirms the hosting environment is ready; this guard predates Person 3's identity work and should be revisited once hosting is actually set up. For deployment, use the reviewed `docs/azure-sql-migration.sql` or EF migrations instead of having every production API instance migrate at startup.

The idempotent deployment script now includes Person 3's `AddUserIdentityFields` and `AddPasswordResetTokens` migrations. It prepares the schema only; it does not enable Production hosting or replace the required deployment configuration review.

After updating the code, restart the **API** in Visual Studio to apply the new migration. `GET /api/v1/jobtemplates/standard` should then return **200** with exactly six checklist items. It adds no demo customers, technicians, users, or jobs; those lists stay empty until records are created. If the template route still returns **404**, confirm the API restarted with the updated build and that `20261003120000_AddStandardJobCardTemplate` appears in `__EFMigrationsHistory`.

## Free AI reporting and PDF downloads

The PDF is generated by the API itself; it needs no paid PDF service or extra storage. It includes the monthly statistics and the same AI commentary shown on the report page when that commentary is available. CSV remains statistics-only. The PDF still downloads if the AI provider is unavailable, with a clear note in place of commentary.

## Use the Postman collection

The collection is `postman/VertecoSmartJob.Person1.postman_collection.json`. It contains **38 request examples**. It has no Postman assertion scripts, so a Runner result of **“0 tests”** means no automated assertions were defined; it does not indicate whether the HTTP responses were correct. Run the requests individually in the order below. Running the entire collection at once will encounter empty IDs, invalid job states, and requests that deliberately change data.

### 1. Import and configure

1. In Postman, choose **Import → File** and select the collection JSON above.
2. Open the imported collection's **Variables** tab and set the values below. Collection variables work even when Postman shows **Environment: none**. Save the collection after editing them.

   | Variable | Value |
   | --- | --- |
   | `baseUrl` | `http://localhost:5196` for Visual Studio's **http** profile. |
   | `managerKey` | The exact `ApiKeys:Accounts[0]:Key` value in your User Secrets. |
   | `customerId` | A customer GUID copied from **List customers** or **Create customer**. |
   | `siteId` | A site GUID belonging to that customer, from **List sites** or **Create site**. |
   | `technicianId` | A technician GUID copied from **List technicians**. |
   | `jobId` | A job GUID copied from **List jobs** or **Create job**; change it when testing a different job state. |
   | `userId` | A user GUID copied from **List users** or the disposable-user request below. |
   | `reportPeriod` | A month in `yyyy-MM` form, for example `2026-09`; use a month containing your test data for nonzero figures. |
   | `correctiveActionId` | Copy an `id` from **List corrective actions** or a flagged job's `correctiveActions` array. |

3. Start the API in Visual Studio and send **Dashboard stats**. Expect **200 OK**. Every collection request already sends `X-SmartJob-Key: {{managerKey}}`; you do not need to log in or add Bearer authentication to use this collection as-is. A missing/wrong key normally gives **401**, while a valid key without the required Manager role gives **403**. To test real login/JWT, call `POST /api/v1/auth/login` manually or use Swagger's Authorize button as described below.
4. In a JSON response, copy an `id` GUID into the relevant collection variable. For list responses, look in `items`. Do not include quotes around a GUID in the variable value. Empty `{{jobId}}`, `{{siteId}}`, or `{{userId}}` values make detail and write requests hit the wrong URL.

### Authenticate in Swagger (Development)

Open `http://localhost:5196/swagger` and click **Authorize**. For **SmartJobKey**, enter the development key value only; Swagger sends it as `X-SmartJob-Key`. For **Bearer**, first call `POST /api/v1/auth/login`, copy `accessToken` from the response, and enter only that token; Swagger adds the `Bearer` prefix. Authorize with one scheme at a time. If both are filled in, the API selects Bearer whenever an `Authorization` header is present. Click **Logout** in the Authorize dialog before switching schemes. Swagger is available only in Development.

### 2. Run the read-only requests

These requests can be sent without changing data. For detail requests, first fill in the corresponding ID variable.

| Collection request | Expected result / what to check |
| --- | --- |
| **Dashboard stats** | **200**; `activeJobCards`, `dueTodayCount`, `techniciansAvailable`. |
| **List jobs** | **200**; `items`, `totalCount`, `page`, `pageSize`. Copy a job's `id`, `status`, and `customerId`/`siteId` from **Get job** when needed. |
| **Get job** | **200** for a valid `jobId`; shows status, standard checklist, and assigned technician if present. |
| **List customers** | **200**; copy an `items[].id` into `customerId`. |
| **Get customer** | **200** for a valid `customerId`; inspect its sites. |
| **List sites** | **200**; set `customerId` first to filter by that customer, then copy a site `id` into `siteId`. |
| **Get site** | **200** for a valid `siteId`; verify its `customerId` matches the selected customer. |
| **Look up equipment serial** | **404** until matching equipment is added. |
| **List users** | **200**; copy a safe test user's `id` into `userId`. |
| **Get user** | **200** for a valid `userId`. |
| **Edit user** | **200** for a valid `userId`; update a disposable user's name, email, and phone. For a linked Technician, the profile is updated too. |
| **List technicians** | **200** with an empty `items` array until a technician is added; use **Create technician and account**. |
| **Standard job card fields** | **200** with the single standard template and six checklist items after the API applies the migration. |
| **List reports** | **200**; edit the request's `year=2026` query parameter if testing another year. A month with no data still returns a report with zero counts. |
| **Monthly report and AI commentary** | **200** for valid `reportPeriod`; check `aiStatus` and `aiCommentary`. Commentary needs the separate Gemini key described above. |
| **Export report CSV** | **200**, `text/csv`; use **Send and Download** in Postman to save the file. |
| **Export report PDF** | **200**, `application/pdf`; use **Send and Download** to save the file. It includes available AI commentary. |
| **Audit trail** | **200**; repeat after writes to see new entries. Now also includes Person 3's auth events (`LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `PasswordResetRequested`, `PasswordReset`, `PasswordSetByManager`) alongside Alex's job-lifecycle entries, in the same table. |
| **List flagged jobs** | **200**; `items` contains only jobs with `isFlagged: true`. |
| **List corrective actions** | **200**; defaults to `status=open`. Copy an `items[].id` into `correctiveActionId`; change `status` to `resolved` to view history. |
| **Get corrective action** | **200** for a valid `correctiveActionId`; shows the reason, job number, and resolution fields. |

### 3. Test customer, site, and job writes in order

Each `POST` or `PATCH` request has a sample JSON body under Postman's **Body → raw → JSON**. Replace the `{{...}}` values via collection variables and edit sample names if you run the request more than once.

The job steps require the Standard Job Card template. Confirm **Standard job card fields** returns **200** before step 5.

| Order | Collection request | Expected result and next step |
| --- | --- | --- |
| 1 | **Create customer** | **201 Created**. Copy response `id` to `customerId`. Use a distinct customer name for repeat runs. |
| 2 | **Edit customer** | **200**. Confirm `contactPhone` changed; **Get customer** should show the same value. |
| 3 | **Create site** | **201 Created**. Its body uses `{{customerId}}`. Copy response `id` to `siteId`. |
| 4 | **Edit site** | **200**. Confirm `address` changed; **Get site** should show it. |
| 5 | **Create job with standard checklist** | **201 Created** after the template migration is applied. `customerId` and `siteId` must match. **Remove the `technicianId` line from this request body** so the new job stays in `Draft`. Copy response `id` to `jobId`. |
| 6 | **Edit draft job** | **200**; verify the `priority` and `notes` in **Get job**. This only works while status is `Draft`. |
| 7 | **Assign technician** | **200** after a job and technician exist. Use **Create technician and account** and copy its `id` to `technicianId`. Once assigned, the draft edit request correctly returns **409 Conflict**. Assigning now also sends the technician an email notification (Person 3) if SMTP is configured — see below. |

The collection's **Approve submitted job** request cannot approve a newly assigned job. A technician must accept it, start it, complete its checklist, add evidence and both signatures, then submit it for review. Use the technician routes and the separate mobile Postman collection described in [the mobile API guide](docs/mobile-api-testing.md) to complete that workflow. The Person 1 collection still covers manager requests.

Next, use that completed job to send **Reopen job**. Expect **200** and status `Reopened`. To test **Cancel active job**, first set `jobId` to a *different* job in `Draft`, `Assigned`, `Accepted`, `InProgress`, `AwaitingApproval`, or `Reopened`; expect **200** and `Cancelled`. Cancelling a `Completed` or already `Cancelled` job correctly returns **409**. These requests change shared Azure data, so choose disposable records when possible.

### 4. Test user management safely

The collection includes **List users**, **Get user**, **Suspend user**, and **Deactivate user**. Do not suspend or deactivate the preserved `manager@verteco.example` account: the development key maps to that user.

To create a disposable account before using the destructive user requests, make a separate Postman request:

- Method: `POST`
- URL: `{{baseUrl}}/api/v1/users`
- Headers: `X-SmartJob-Key: {{managerKey}}` and `Content-Type: application/json`
- Body → raw → JSON:

  ```json
  {
    "fullName": "Postman Test Manager",
    "email": "postman-test-unique@example.test",
    "phone": "011 000 0000",
    "role": "Manager"
  }
  ```

Use a new email on repeat runs. Expect **201** and copy the returned `id` into `userId`. Then send **Get user** (**200**), **Edit user** (**200**), **Suspend user** (**200**, `isSuspended: true`), and **Deactivate user** (**204 No Content**). Change the Edit user's sample email to another unused address on repeat runs. An empty response body after **204** is correct. The Manager-only `PATCH /api/v1/users/{id}` route already existed when Person 3's branch was merged; it also synchronizes a linked Technician's name, email, and phone.

**Note (Person 3):** a newly created user — whether via this request or **Create technician and account** below — has **no password** until a Manager explicitly sets one via `POST /api/v1/auth/users/{id}/set-password`. They cannot log in until that happens. See Person 3's section for details.

### 5. Test technician creation and corrective actions

**Create technician and account** sends `POST /api/v1/technicians` as a Manager. Change the sample email before repeating it. Expect **201 Created** with both `id` (the new `technicianId`) and `userId` (the linked account for `/api/v1/users/{id}` suspend/reactivate/deactivate routes). The Technician and User are saved together. **Do not** call `POST /api/v1/users` again for this technician. Set the collection's `technicianId` to the returned `id`, then send **Edit technician** (`PATCH /api/v1/technicians/{{technicianId}}`); expect **200** and verify skills/availability. Name, email, and phone edits also update the linked User. The new endpoints require a Manager key.

For a flagged-work walkthrough, set `jobId` to an existing job that is not Cancelled. Send **Flag job** with a nonempty `reason`; expect **200** with `isFlagged: true`, `flagReason`, and one new entry in `correctiveActions`. Managers can flag a job; an authenticated Technician may flag only a job assigned to them. **List flagged jobs** (`GET /api/v1/jobs?flagged=true`) should now contain this job. **List corrective actions** with `status=open` should contain its action. Copy that action's `id` into `correctiveActionId`, then send **Get corrective action** to inspect it.

Resolve the flag using **either** **Resolve corrective action** (`POST /api/v1/corrective-actions/{{correctiveActionId}}/resolve`) **or** **Resolve job flag** (`POST /api/v1/jobs/{{jobId}}/resolve-flag`). Both take a nonempty `resolutionNotes` body and require a Manager key. Expect **200**; the job's `isFlagged` becomes `false`, and the corrective action remains in history with its resolution. Change **List corrective actions** to `status=resolved` to see it. Resolving the same action twice returns **409 Conflict**. A job in `AwaitingApproval` cannot be approved while it has an open flag; resolve it first.

Each flag creates one corrective-action record with a reason, open/resolved state, timestamps, actor IDs, and resolution notes. This is the minimal persisted workflow for Person 2's Flagged Items and Corrective Actions screens; it does not yet include independent tasks, assignees, or due dates. A new EF migration adds these fields and records. The API applies it on the next Development startup; deployed databases need the updated migration script.

### Troubleshooting Postman and Azure

| Symptom | Check |
| --- | --- |
| API stops at `MigrateAsync`, SQL host not found | Use the exact `*.database.windows.net` server name in User Secrets; check internet/DNS and Azure SQL Networking. |
| SQL login or firewall error | Check SQL login/password, database name, your current public IP firewall rule, and whether the free database has paused after its allowance. |
| API says `ConnectionStrings:AzureSql` or `Blob:ServiceUri or Blob:ConnectionString` is required | Open **Manage User Secrets** on the **API project**, verify the JSON keys and run with the **Development** launch profile. |
| Postman `401` | Confirm the API is running and `managerKey` exactly matches User Secrets; the email must identify an existing, active user. If testing with a Bearer token instead, confirm the token hasn't expired (30-minute lifetime) and that `Authorization: Bearer <token>` is actually present on the request — Postman's Authorization tab can silently fail to apply if a conflicting header is also set manually; check the Headers tab directly if in doubt. |
| Web dashboard redirects to login after its API token expires | Sign in again with the Manager account. The Web cookie can outlive its API token; the Web app now clears an invalid cookie when the API returns `401`. If a fresh login still fails, check the API's `JwtBearer` authentication warning and verify `Api:BaseUrl` points to the API's HTTPS address. |
| Postman `403` | The key or token identified a user without permission for that manager endpoint — this is expected and correct for a Technician token hitting a Manager-only route. |
| Postman `400` | Check required body fields, valid GUID variables, JSON syntax, and the `yyyy-MM-dd` job date or `yyyy-MM` report period. |
| Postman `404` or `405` on an ID request | Fill `jobId`, `customerId`, `siteId`, or `userId`; an empty variable can turn a detail URL into a collection URL. |
| Postman `409` | Check the job's current `status` and flag state; edits require `Draft`, approval requires `AwaitingApproval` with no open flag, and a flag can be resolved only once. |
| Postman `423` | The account is locked out after 5 failed login attempts; wait 15 minutes or have a Manager reset it via `set-password`. This is correct, intended behaviour — see Person 3's section. |
| Postman Runner says **0 tests** | The collection has request examples but no Postman assertion scripts. Inspect response status/body for each request using the tables above. |

## Person 2 (Caleb) — Web Front End setup

This section documents what's been done to get `VertecoSmartJob.Web` built and running against this API, for the rest of the team.

### What's included

A server-rendered ASP.NET Core MVC front end, calling this API through typed `HttpClient`s rather than any JavaScript framework. Screens built so far:

- **Dashboard** — summary stats (active job cards, due today, technicians available).
- **Job Cards** — list/filter, create (with the one seeded Standard checklist attached automatically), and a details view with the lifecycle tracker (Draft → Assigned → Accepted → InProgress → AwaitingApproval → Completed, plus Reopened/Cancelled).
- **Customers** — list, details (with its sites), create, edit; create/edit also support creating a new site inline against that customer.
- **Sites** — list, details (with equipment), create, edit.
- **Technicians** — list, details (with linked user account status and Suspend/Reactivate/Deactivate actions), create, edit.
- **Flagged Items** — list of flagged jobs and their open corrective actions (Manager only).
- **Historical Records** — searchable record view across jobs/equipment history.
- **Reports** — monthly report list and a details page with stats, status breakdown, AI commentary, and CSV/PDF export links (Manager only).
- **Activity Feed** — audit trail view (Manager only).
- **Profile & Settings** — read-only signed-in user info and logout.
- **Login** — real email/password sign-in against the Api's `/api/v1/auth/login` endpoint. See Person 3's section below for how accounts get their first password and how the identity system works end to end.
- **Privacy Notice** — a POPIA-aligned privacy page (Person 3), linked from the sidebar under Account.

Manager-only pages and nav items are hidden from Technician accounts via `User.IsInRole("Manager")` checks, matching the API's own role rules.

### Project structure — how to navigate

Everything below lives inside `src/VertecoSmartJob.Web/`:

| Folder / file | What's in it |
| --- | --- |
| `Program.cs` | App startup — registers one typed `HttpClient` per API area, cookie auth setup, MVC routing, and (Person 3) a security response headers middleware. |
| `Auth/` | `ApiKeyDelegatingHandler.cs` — attaches the signed-in user's JWT as a `Bearer` token to every outgoing API request (Person 3; the file name is unchanged from the original API-key version, but it now carries a real token read from the sign-in cookie rather than a static shared key). |
| `Api/` | One typed client class per API area (`JobCardsApiClient`, `CustomersApiClient`, `SitesApiClient`, `TechniciansApiClient`, `UsersApiClient`, `DashboardApiClient`, `ReportsApiClient`, `AuditApiClient`, `CorrectiveActionsApiClient`, `JobTemplatesApiClient`), plus `AuthApiClient.cs` (Person 3 — calls `/api/v1/auth/login`; deliberately does **not** use `ApiKeyDelegatingHandler`, since logging in is the one call made before any credential exists), and shared plumbing (`ApiException.cs`, `ApiResponseExtensions.cs`, `AuditEntryDto.cs`, `JobCardListQuery.cs`). Controllers never call the API directly — they go through these. |
| `Models/` | View models for forms and list/detail pages (one per screen, e.g. `JobCardListViewModel.cs`, `TechnicianDetailsViewModel.cs`, `CustomerFormViewModel.cs`, `LoginViewModel.cs`). |
| `Controllers/` | One controller per feature area — `HomeController` (Dashboard), `JobCardsController`, `CustomersController`, `SitesController`, `TechniciansController`, `FlaggedItemsController`, `HistoryController`, `ReportsController`, `ActivityController`, `ProfileController`, `AccountController` (real login/logout, Person 3), `PrivacyController` (Person 3). |
| `Views/{ControllerName}/` | The Razor pages for that controller — folder names match controller names exactly (e.g. `Views/Technicians/` holds `Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`). |
| `Views/Shared/` | `_Layout.cshtml` (the sidebar/topbar shell every page renders inside) and `Error.cshtml`. |
| `Views/_ViewStart.cshtml`, `Views/_ViewImports.cshtml` | Wires every view to `_Layout.cshtml` and brings in shared `@using`s and tag helpers. |
| `wwwroot/css/site.css` | All styling — colors, layout, the login screen, sidebar, cards, tables, etc. |
| `wwwroot/js/site.js` | Sidebar toggle, toast notifications, and the inline customer/site creation panels on the job card form. |
| `wwwroot/images/` | `verteco-logo-full.png` and `verteco-mark-transparent.png`. |
| `appsettings.json` | Non-secret config — `Api:BaseUrl` only. (The old `DevAccounts` dev-login placeholder has been removed — see Person 3's section.) |

To find the code behind any page: open the Razor view under `Views/{Area}/`, then its controller action under `Controllers/{Area}Controller.cs`, then whichever `Api/*ApiClient.cs` it calls for data.

### Using the running app

1. Sign in at `/Account/Login` with a real SmartJob email and password. See Person 3's section below for how an account gets its first password set.
2. You land on the **Dashboard**. The left sidebar groups everything:
   - **Overview** → Dashboard.
   - **Jobs** → Job Cards (list), Create Job Card (Manager only), Flagged Items (Manager only), Historical Records.
   - **Clients** → Customers, Sites.
   - **Team** → Technicians.
   - **Insights** (Manager only) → Reports, Activity Feed.
   - **Account** → Profile & Settings, Privacy Notice, Log out.
3. On mobile-width screens the sidebar collapses behind a hamburger button (top-left).
4. Signing in as a `Technician`-role account hides the Manager-only items automatically.

### Project location and reference

`VertecoSmartJob.Web` lives directly inside `src/`, as a sibling of `VertecoSmartJob.Api`, `VertecoSmartJob.Application`, `VertecoSmartJob.Contracts`, `VertecoSmartJob.Domain`, and `VertecoSmartJob.Infrastructure` — not nested inside an extra `src/src/...` folder, and not at the solution root. Its `.csproj` has exactly one project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\VertecoSmartJob.Contracts\VertecoSmartJob.Contracts.csproj" />
</ItemGroup>
```

A duplicated/incorrect second `ProjectReference` line (pointing at `..\src\VertecoSmartJob.Contracts\...`) will cause `NU1104`/`MSB9008` build errors and a cascade of unrelated-looking `CS1061` errors in the Views — if you see that, check this file first.

Separately: `VertecoSmartJob.slnx` should list every project **once**, each under the correct folder (e.g. `src/VertecoSmartJob.Web/VertecoSmartJob.Web.csproj`). A stray duplicate entry with the wrong path (missing the `src/` prefix) has shown up before and causes Visual Studio to display a second, broken "(not found)" copy of a project in Solution Explorer alongside the real one. If you see this, open `VertecoSmartJob.slnx` in a text editor and remove the incorrectly-pathed duplicate line — do not remove the correct one.

### Running the Web app against the Api

- `Web/appsettings.json` points `Api:BaseUrl` at `https://localhost:7086` (the Api's **https** profile — see the note below on why https specifically matters).
- To run both projects together from one Visual Studio instance: right-click the solution → **Configure Startup Projects…** → **Multiple startup projects** → set both `VertecoSmartJob.Api` and `VertecoSmartJob.Web` to **Start**, then F5.
- Person 3's note: point `Api:BaseUrl` at the Api's **https** URL, not http. The Api redirects http → https (`UseHttpsRedirection`), and .NET's `HttpClient` strips the `Authorization` header when a request is redirected across a scheme/port boundary, as a security safeguard. Pointing at http will cause every authenticated request from the Web app to silently lose its Bearer token mid-redirect, which looks like a confusing, intermittent `401` rather than an obvious configuration error.
- If you're only running Web locally against an Api someone else is hosting, you no longer need any shared secret between the two for login itself — the Web app simply calls that Api's real `/api/v1/auth/login` endpoint and uses whatever token comes back. You do still need the Api's database/storage secrets if you're running the Api yourself rather than pointing at someone else's running instance.

### Static assets

`wwwroot/css/site.css` and the two logo images in `wwwroot/images/` (`verteco-logo-full.png`, `verteco-mark-transparent.png`) must exist in the Web project for the UI to render with its intended styling. These are plain static files, not generated at build time — if the site loads with no styling or broken image icons, check these are actually present first.

### Known local setup issue: Azure SQL firewall

If the Api throws something like:

```
Microsoft.Data.SqlClient.SqlException: Cannot open server 'smartjob' requested by the login.
Client with IP address 'x.x.x.x' is not allowed to access the server.
```

this is Azure SQL's firewall rejecting your public IP — not a code or configuration problem on the Web side. Whoever owns the Azure SQL server needs to add that IP under **Azure Portal → SQL servers → `smartjob` → Networking → Firewall rules**, which can take a few minutes to take effect. Since the Api won't start at all while this is unresolved, any Web page that depends on real data will hang or fail until it's fixed. Home/mobile IPs can change over time, so this may need to be re-added later.

## Person 3 (Moe) — Security, Identity & Notifications

This section documents the real authentication system, password reset flow, audit/notification wiring, and security hardening built on top of Persons 1 and 2's work — replacing the temporary dev-account login bridge that the Web app originally shipped with.

### What's included

**Identity core**
- `User` (Domain) extended with `PasswordHash`, `IsEmailConfirmed`, `FailedLoginAttempts`, and `LockedOutUntilUtc` — all behind behavior methods (`SetPasswordHash`, `ConfirmEmail`, `RegisterFailedLogin`, `ResetFailedLogins`, `IsCurrentlyLockedOut`), never public setters.
- `PasswordHashingService` (Infrastructure) — PBKDF2 hashing via ASP.NET Core's built-in `PasswordHasher<User>`. Salted automatically; the same password hashed twice never produces the same stored value.
- `JwtTokenService` (Infrastructure) — issues signed, short-lived (30 minute) access tokens carrying `ClaimTypes.NameIdentifier`, `Name`, `Email`, `Role`, and a `TechnicianId` claim when applicable. This is deliberately the **same claim shape** Alex's `ApiKeyAuthenticationHandler` already produces, so every existing `[Authorize(Roles=...)]` check and `ActorTechnicianId`/`ICurrentUser` lookup across the codebase works unchanged regardless of which credential type authenticated the request.
- `AuthController` (`/api/v1/auth`): `POST /login`, `POST /forgot-password`, `POST /reset-password`, and `POST /users/{id}/set-password` (Manager-only).
- Dual authentication in the Api's `Program.cs`: a policy scheme forwards to JWT Bearer validation when a request carries an `Authorization` header, or to Alex's existing API-key scheme otherwise. Both are fully supported side by side — Postman's `X-SmartJob-Key` testing flow still works exactly as documented above, and the Web app now uses the JWT path.

**Password reset, with real email**
- `PasswordResetToken` (Domain) — a single-use, time-limited (1 hour) token. The emailed link contains the raw token; only its SHA-256 hash is ever stored, same principle as the password itself.
- `IEmailSender` / `SmtpEmailSender` (Infrastructure) — sends real email via Gmail SMTP. Failures are logged, not thrown, so a flaky mail server never blocks the calling request.
- `POST /api/v1/auth/forgot-password` always returns the same response whether or not the email exists, to avoid leaking which emails are registered. `POST /api/v1/auth/reset-password` validates the token, updates the password, and marks the token used.
- This has been tested end to end: a real password reset email was sent and received, and the returned link's token successfully completed a password change.

**Lockout and rate limiting**
- 5 failed login attempts locks the account for 15 minutes (`423 Locked`), even if the correct password is then supplied during that window.
- A dedicated `"auth"` rate-limit policy (10 requests / 5 minutes) applies to `login`, `forgot-password`, `reset-password`, and `set-password`.
- This has been tested directly: 5 wrong-password attempts in a row produced a real `423` on the 6th, confirmed in the audit log.

**Audit trail and notifications**
- Auth events (`LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `PasswordResetRequested`, `PasswordReset`, `PasswordSetByManager`) write into the same `AuditLog` table Alex's job-lifecycle code already uses, via `IAuditLogRepository` — one unified audit trail, not two separate logs.
- `INotificationService` / `EmailNotificationService` (wired into `JobCardService`) sends a real email when a job is assigned, submitted for review, approved, reopened, or flagged. Technicians are emailed directly (via their linked `User.Email`); Managers are emailed as a group for events needing their review. This has been tested by assigning a real job and confirming no send failure was logged.

**Web app integration**
- `AccountController` now calls the real `/api/v1/auth/login` endpoint via a new `AuthApiClient`, storing the returned JWT in the sign-in cookie instead of the old static API key. The old `DevAccount.cs` model and the `DevAccounts` config sections have been removed — they're no longer referenced anywhere.
- `ApiKeyDelegatingHandler` (same file name, different contents) now attaches `Authorization: Bearer <token>` from the signed-in cookie to every outgoing API call, instead of a static `X-SmartJob-Key`.

**Hardening**
- CSRF: every mutating Web controller action already had `[ValidateAntiForgeryToken]` (confirmed by checking all 12 controllers — every `[HttpPost]` has a matching token check).
- SQL injection: confirmed no raw SQL anywhere in the codebase; everything goes through EF Core's parameterized queries.
- Security response headers added to both apps: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin` on both the Api and Web app, plus a `Content-Security-Policy` on the Web app specifically (the Api is a pure JSON API and doesn't render HTML, so CSP doesn't meaningfully apply there except to its dev-only Swagger UI).
  - The Web app's CSP allows `'unsafe-inline'` for scripts and styles. This is a deliberate, documented trade-off: the app already uses inline `<script>` (on the Create Job Card page), inline `onclick=` handlers, and inline `style=""` attributes across many views. A stricter, nonce-based CSP is the textbook-correct approach but would require refactoring those into external files — flagged here as a known limitation rather than hidden.
- A `JwtBearer` authentication-failure logger was added (`OnAuthenticationFailed` in `Program.cs`) so a rejected token now produces a diagnostic log line instead of a silent `401` with no explanation.
- Found and fixed a real authorization bug along the way: `TechniciansController.Suspend` (Web) was missing `[Authorize(Roles = "Manager")]`, unlike its two neighboring actions (`Reactivate`, `Deactivate`). The Api's own `UsersController` still had a class-level Manager-only guard, so this was never actually exploitable end to end, but it's a good example of defense-in-depth catching a gap in one layer.

**POPIA / privacy**
- A **Privacy Notice** page (`/Privacy`, `PrivacyController` + `Views/Privacy/Index.cshtml`) covering what's stored, why (including the client-confirmed indefinite retention policy, for warranty claims and renewals), and how it's protected. Linked from the sidebar under Account. Anonymous-accessible by design, since a privacy notice should be readable without needing to log in first.

### How to actually log in for the first time

The existing Manager login has a working password. Any new account has no password until a Manager sets one:

1. Run the Api and Postman's **List users** to find the target user's `id` (a `User` id, not a `Technician` id — they're different GUIDs for the same person).
2. As a Manager (via `managerKey` or a Manager's own JWT), call:
   ```
   POST {{baseUrl}}/api/v1/auth/users/{id}/set-password
   { "newPassword": "SomeStrongPassword123!" }
   ```
   Expect `204 No Content`. This also auto-confirms the email and clears any lockout state on that account.
3. That person can now log in for real at `/Account/Login` (Web) or `POST /api/v1/auth/login` (API directly), using that email and password.
4. From then on, they can use `POST /api/v1/auth/forgot-password` to reset it themselves via a real emailed link, without needing a Manager's help again.

### Gmail SMTP setup (for anyone who needs to run email-dependent features locally)

1. Use a dedicated Gmail account for this project (not a personal one) — this is what sends every notification and reset email, and the email will visibly come from this address.
2. Enable **2-Step Verification** on that Google account (Security settings).
3. Generate an **App Password** at `myaccount.google.com/apppasswords` — this 16-character value, not the normal Gmail password, goes in `Smtp:Password` in User Secrets.
4. Without this configured, email-dependent features (reset, notifications) don't throw — they log a warning and continue, so the rest of the app still works, but no real email goes out.

### New/changed API endpoints (Person 3)

| Endpoint | Method | Auth | Notes |
| --- | --- | --- | --- |
| `/api/v1/auth/login` | POST | None | Returns a JWT, `fullName`, `role`. `423` if locked out, `401` on bad credentials, `403` if email unconfirmed. |
| `/api/v1/auth/forgot-password` | POST | None | Always returns the same generic message; sends a real email if the address is registered. |
| `/api/v1/auth/reset-password` | POST | None | Takes the raw token from the emailed link and a new password. |
| `/api/v1/auth/users/{id}/set-password` | POST | Manager | Admin-driven password set; also confirms email and clears lockout. |

### Security testing evidence on file

These were verified directly, not just implemented:
- **Lockout:** 5 consecutive wrong-password attempts against the same account produced `423 Locked` on the 6th, and the correct password was also rejected with `423` during the lockout window. Confirmed in the audit log (`LoginFailed` ×5, then `AccountLockedOut`).
- **Role enforcement:** a Technician's valid, correctly-signed JWT was used against a Manager-only endpoint (`GET /api/v1/users`) and correctly received `403 Forbidden`.
- **XSS:** `<script>alert('xss')</script>` entered into a job's notes field rendered as literal encoded text in the page's actual HTML source, not as an executable script — confirmed via View Page Source, not just the browser's rendered view.

### Known limitations / handed to Alex

A separate document (`PERSON3_HANDOFF_TO_ALEX.md`) lists everything Person 3 changed in Alex's files. The user edit route in that handoff is already present, and Swagger now supports both authentication methods. Hosting remains pending: the Production startup guard is still in `Program.cs` until deployment settings and migration rollout are decided.
