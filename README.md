# Verteco SmartJob

Person 1 (Alex)

This folder is the backend, API, and reporting starting point for the group project. It contains Domain, Application, Infrastructure, API, and Contracts projects so the web and mobile teams can share DTOs.

All jobs use the **single seeded Standard Job Card** and its six checklist items. Clients do not choose or edit a template.

## What's included

- Guarded job card lifecycle: Draft → Assigned → Accepted → InProgress → AwaitingApproval → Completed, with Reopened and Cancelled branches.
- Submission validation for required fields, evidence, and both signatures; SHA-256 revision snapshots linked across resubmissions.
- Customer, site, user, and technician management services, soft deletion, and audit records.
- Flagged jobs and persisted corrective actions with manager resolution and audit records.
- Azure SQL EF Core provider, seed data, migrations, and an idempotent SQL deployment script.
- Private Azure Blob Storage adapter. The technician upload and sync HTTP routes belong to Person 4 and are not in this collection yet.
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
     }
   }
   ```

   The `Email` must match the seeded manager user. Choose a long random development key and use its **exact value** in Postman's `managerKey` collection variable. The key is for this local development authentication harness, not your Azure SQL password or Azure account password.
3. Save `secrets.json`, set **VertecoSmartJob.Api** as the startup project, select its **http** launch profile, and press **F5**. The API uses `http://localhost:5196`; Swagger is at `http://localhost:5196/swagger`. The **https** profile also exposes `https://localhost:7086`. The launch profiles set `ASPNETCORE_ENVIRONMENT=Development`, which makes User Secrets available.

User Secrets live outside the repository and everyone configures their own local file. They are intended for development and are not encrypted. Never put real passwords, storage keys, or API keys in `appsettings.json`, the README, the Postman collection, or a commit. 
Development startup applies pending Azure SQL EF migrations and seeds a manager, technicians, customers, sites, equipment, jobs, and the single standard template. The database must be reachable before the API can start. If the SQL firewall or credentials are wrong, startup stops during migration. The development API-key handler allows role checks while Person 3 builds real identity. Production startup is intentionally blocked until that work is integrated. For deployment, use the reviewed `docs/azure-sql-migration.sql` or EF migrations instead of having every production API instance migrate at startup.

## Use the Postman collection

The collection is `postman/VertecoSmartJob.Person1.postman_collection.json`. It contains **37 request examples**. It has no Postman assertion scripts, so a Runner result of **“0 tests”** means no automated assertions were defined; it does not indicate whether the HTTP responses were correct. Run the requests individually in the order below. Running the entire collection at once will encounter empty IDs, invalid job states, and requests that deliberately change data.

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

3. Start the API in Visual Studio and send **Dashboard stats**. Expect **200 OK**. Every collection request already sends `X-SmartJob-Key: {{managerKey}}`; you do not need to log in or add Bearer authentication. A missing/wrong key normally gives **401**, while a valid key without the required Manager role gives **403**.
4. In a JSON response, copy an `id` GUID into the relevant collection variable. For list responses, look in `items`. Do not include quotes around a GUID in the variable value. Empty `{{jobId}}`, `{{siteId}}`, or `{{userId}}` values make detail and write requests hit the wrong URL.

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
| **Look up equipment serial** | **200** for seeded serial `UPS-60-MID-001`; an unknown serial gives **404**. |
| **List users** | **200**; copy a safe test user's `id` into `userId`. |
| **Get user** | **200** for a valid `userId`. |
| **List technicians** | **200**; copy an `items[].id` into `technicianId`. |
| **Standard job card fields** | **200**; one standard template with six checklist items. |
| **List reports** | **200**; edit the request's `year=2026` query parameter if testing another year. A month with no data still returns a report with zero counts. |
| **Monthly report and AI commentary** | **200** for valid `reportPeriod`; check counts and averages. `aiCommentary` may be null when no Gemini key is configured. |
| **Export report CSV** | **200**, `text/csv`; use **Send and Download** in Postman to save the file. |
| **Export report PDF** | **200**, `application/pdf`; use **Send and Download** to save the file. |
| **Audit trail** | **200**; repeat after writes to see new entries. |
| **List flagged jobs** | **200**; `items` contains only jobs with `isFlagged: true`. |
| **List corrective actions** | **200**; defaults to `status=open`. Copy an `items[].id` into `correctiveActionId`; change `status` to `resolved` to view history. |
| **Get corrective action** | **200** for a valid `correctiveActionId`; shows the reason, job number, and resolution fields. |

### 3. Test customer, site, and job writes in order

Each `POST` or `PATCH` request has a sample JSON body under Postman's **Body → raw → JSON**. Replace the `{{...}}` values via collection variables and edit sample names if you run the request more than once.

| Order | Collection request | Expected result and next step |
| --- | --- | --- |
| 1 | **Create customer** | **201 Created**. Copy response `id` to `customerId`. Use a distinct customer name for repeat runs. |
| 2 | **Edit customer** | **200**. Confirm `contactPhone` changed; **Get customer** should show the same value. |
| 3 | **Create site** | **201 Created**. Its body uses `{{customerId}}`. Copy response `id` to `siteId`. |
| 4 | **Edit site** | **200**. Confirm `address` changed; **Get site** should show it. |
| 5 | **Create job with standard checklist** | **201 Created**. `customerId` and `siteId` must match. **Remove the `technicianId` line from this request body** so the new job stays in `Draft`. Copy response `id` to `jobId`. The API automatically uses the one standard job card template. |
| 6 | **Edit draft job** | **200**; verify the `priority` and `notes` in **Get job**. This only works while status is `Draft`. |
| 7 | **Assign technician** | **200**; set `technicianId` from **List technicians**, then verify status `Assigned`. Once assigned, the draft edit request correctly returns **409 Conflict**. |

The collection's **Approve submitted job** request cannot approve a newly assigned job. A technician must accept it, start it, complete its checklist, add evidence and both signatures, then submit it for review. Those technician HTTP routes are not yet in the Person 1 collection. For a manager-side test, use a **seeded `AwaitingApproval` job** from **List jobs**, set its `id` as `jobId`, then send **Approve submitted job**. Expect **200** and status `Completed`.

Next, use that completed job to send **Reopen job**. Expect **200** and status `Reopened`. To test **Cancel active job**, first set `jobId` to a *different* job in `Draft`, `Assigned`, `Accepted`, `InProgress`, `AwaitingApproval`, or `Reopened`; expect **200** and `Cancelled`. Cancelling a `Completed` or already `Cancelled` job correctly returns **409**. These requests change shared Azure data, so choose disposable records when possible.

### 4. Test user management safely

The collection includes **List users**, **Get user**, **Suspend user**, and **Deactivate user**. Do not suspend or deactivate the seeded `manager@verteco.example` account: the development key maps to that user.

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

Use a new email on repeat runs. Expect **201** and copy the returned `id` into `userId`. Then send **Get user** (**200**), **Suspend user** (**200**, `isSuspended: true`), and **Deactivate user** (**204 No Content**). An empty response body after **204** is correct.

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
| Postman `401` | Confirm the API is running and `managerKey` exactly matches User Secrets; the email must identify an existing, active user. |
| Postman `403` | The key identified a user without permission for that manager endpoint. |
| Postman `400` | Check required body fields, valid GUID variables, JSON syntax, and the `yyyy-MM-dd` job date or `yyyy-MM` report period. |
| Postman `404` or `405` on an ID request | Fill `jobId`, `customerId`, `siteId`, or `userId`; an empty variable can turn a detail URL into a collection URL. |
| Postman `409` | Check the job's current `status` and flag state; edits require `Draft`, approval requires `AwaitingApproval` with no open flag, and a flag can be resolved only once. |
| Postman Runner says **0 tests** | The collection has request examples but no Postman assertion scripts. Inspect response status/body for each request using the tables above. |

## Person 2 (Caleb) — Web Front End setup

This section documents what's been done to get `VertecoSmartJob.Web` built and running against this API, for the rest of the team.

### Project location and reference

`VertecoSmartJob.Web` lives directly inside `src/`, as a sibling of `VertecoSmartJob.Api`, `VertecoSmartJob.Application`, `VertecoSmartJob.Contracts`, `VertecoSmartJob.Domain`, and `VertecoSmartJob.Infrastructure` — not nested inside an extra `src/src/...` folder, and not at the solution root. Its `.csproj` has exactly one project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\VertecoSmartJob.Contracts\VertecoSmartJob.Contracts.csproj" />
</ItemGroup>
```

A duplicated/incorrect second `ProjectReference` line (pointing at `..\src\VertecoSmartJob.Contracts\...`) will cause `NU1104`/`MSB9008` build errors and a cascade of unrelated-looking `CS1061` errors in the Views — if you see that, check this file first.

### Save local settings in Visual Studio User Secrets (Web project)

1. In Solution Explorer, right-click **VertecoSmartJob.Web** (not the solution, not the Api project) → **Manage User Secrets**.
2. Add a `DevAccounts` array — one entry per person who needs to sign in during development:

   ```json
   {
     "DevAccounts": [
       {
         "Email": "manager@verteco.example",
         "FullName": "SmartJob Manager",
         "Role": "Manager",
         "ApiKey": "<the exact key Person 1 set for this email under ApiKeys:Accounts>"
       }
     ]
   }
   ```

3. The login page has no real password check yet (Person 3 hasn't built identity) — entering any password with an email that matches a `DevAccounts` entry signs you in as that user. Each entry's `ApiKey` must exactly match the `ApiKeys:Accounts[].Key` value for that same `Email` in the **Api** project's own User Secrets (above), since that key is what's sent on every request so the Api can authorize you as that user. Agree on these key values directly with whoever owns the Api's secrets — they're never committed to git.

### Running the Web app against the Api

- `Web/appsettings.json` points `Api:BaseUrl` at `http://localhost:5196`, matching the Api's **http** launch profile.
- To run both projects together from one Visual Studio instance: right-click the solution → **Configure Startup Projects…** → **Multiple startup projects** → set both `VertecoSmartJob.Api` and `VertecoSmartJob.Web` to **Start**, then F5.
- If you're only running Web locally against an Api someone else is hosting, you only need matching `ApiKeys:Accounts` values with them — not the Api's database/storage secrets.

### Static assets

`wwwroot/css/site.css` and the two logo images in `wwwroot/images/` (`verteco-logo-full.png`, `verteco-mark-transparent.png`) must exist in the Web project for the UI to render with its intended styling. These are plain static files, not generated at build time — if the site loads with no styling or broken image icons, check these are actually present first.

### Known local setup issue: Azure SQL firewall

If the Api throws something like:

```
Microsoft.Data.SqlClient.SqlException: Cannot open server 'smartjob' requested by the login.
Client with IP address 'x.x.x.x' is not allowed to access the server.
```

this is Azure SQL's firewall rejecting your public IP — not a code or configuration problem on the Web side. Whoever owns the Azure SQL server needs to add that IP under **Azure Portal → SQL servers → `smartjob` → Networking → Firewall rules**, which can take a few minutes to take effect. Since the Api won't start at all while this is unresolved, any Web page that depends on real data will hang or fail until it's fixed. Home/mobile IPs can change over time, so this may need to be re-added later.
