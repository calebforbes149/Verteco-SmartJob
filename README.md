# Verteco SmartJob

Verteco SmartJob is a digital field-service and job-card system. Managers use an ASP.NET Core MVC web app to plan and review work. Technicians use a .NET MAUI Android app to complete it, capture evidence and obtain sign-off. Both clients share an ASP.NET Core Web API backed by Azure SQL and private Azure Blob Storage.

**Module:** INSY7315 - Information Systems 3E, Work Integrated Learning  
**Client:** Verteco  
**Group:** 20 - Team Vantage

## Project links

| Resource | Link or location |
| --- | --- |
| Manager web app | [Open the hosted SmartJob web console](https://verteco-smartjob-web-ctatfjduapbcdzb5.southafricanorth-01.azurewebsites.net) |
| API liveness check | [Open /health/live](https://verteco-smartjob-api-hcdyfgf4cagdh2db.southafricanorth-01.azurewebsites.net/health/live) - confirms the API process is running. |
| API readiness check | [Open /health/ready](https://verteco-smartjob-api-hcdyfgf4cagdh2db.southafricanorth-01.azurewebsites.net/health/ready) - checks access to Azure SQL and Blob Storage. |
| Android installer | The signed APK is embedded in the [user guide](Documents/Verteco%20SmartJob%20User%20Guide.pdf). Team members can also use [GitHub Releases](https://github.com/EMGPRS/insy7315-2026-insy7315-2026-final-project-st10453511/releases). |
| User guide | [Verteco SmartJob User Guide](Documents/Verteco%20SmartJob%20User%20Guide.pdf) |
| Video demonstration | [Watch the SmartJob project video](https://youtu.be/Vq9oaBn_71Y?si=rwpBA2ztyZ8n7VZx) |
| Part 3 final report | [Verteco Final Report (Part 3).docx](Documents/Verteco%20Final%20Report%20%28Part%203%29.docx) |
| Part 1 full documentation with feedback | [Part 1 Report (With Feedback Implementation).docx](Documents/Part%201%20Report%20%28With%20Feedback%20Implementation%29.docx) |
| Implementation presentation | [Verteco SmartJob Presensation.pptx](Documents/Verteco%20SmartJob%20Presensation.pptx) |
| Expo presentation | [Verteco SmartJob Expo Presentation.pptx](Documents/Verteco%20SmartJob%20Expo%20Presentation.pptx) |
| Submission links document | [Links.pdf](Documents/Links.pdf) |
| Attendance record | [Attendance.pdf](Documents/Attendance.pdf) |
| iOS release guide | [iOS build and release steps](Documents/ios-build-and-release.md) |
| API test collections | [Manager API collection](postman/VertecoSmartJob.Person1.postman_collection.json) and [mobile API collection](postman/VertecoSmartJob.Mobile.postman_collection.json) |
| Task 2 Repository (Showcase branches and main protection) | [EMGPRS SmartJob Task 2 repository](https://github.com/EMGPRS/insy7315-2026-task-2-st10453511.git)|

The Task 2 repository above is where all six members developed their contributions on separate branches. Changes went through pull requests for review, and an administrator approved merges into `main`. This final-project repository contains the integrated code and submission documents. Branch approval is a GitHub repository rule; it is separate from the Actions workflows described below.

## Demo sign-in

| App | Sign-in email | Password | Access |
| --- | --- | --- | --- |
| Manager web console | `alexxornelass755@gmail.com` | `Test@verteco` | Open the hosted web app above and select **Sign in**. |
| Technician Android app | `caleb@gmail.com` | `password123` | Install the signed APK from the user guide or a team-accessible release, then sign in on the phone. |

The web account has the Manager role. The Android account has the Technician role and sees work assigned to that technician. These are demonstration credentials for the project walkthrough.

## Group members and roles

| Person section | Member | Student number | Group | Project role | Contribution documented below |
| --- | --- | --- | --- | --- | --- |
| Person 1 | Alexandro Ornelas | ST10437348 | Group 1 | Backend Core Lead | Backend, API and reporting |
| Person 2 | Caleb Forbes | ST10438433 | Group 1 | Web Front End Lead | Manager web app |
| Person 3 | Moegammad Alexander | ST10453511 | Group 2 | Group Leader, Project Manager and Data Lead | Security, identity and notifications |
| Person 4 | Isaac Phiri | ST10074970 | Group 3 | DevOps, Hosting and API Platform Lead | Mobile backend, API and sync |
| Person 5 | Erin Sarjoo | ST10450571 | Group 2 | Mobile Lead | Technician mobile front end |
| Person 6 | Keeran Perumal | ST10367702 | Group 3 | Security and Identity Lead | Mobile security, DevOps and hosting |

## CI/CD and deployment

The workflows below are part of this repository. GitHub Actions checks pull requests and branch pushes, while repository rules control who can merge. The team worked on separate branches and needed administrator approval before merging into `main`. A workflow does not grant that approval. In a repository with these workflows, a merge to `main` creates a push that starts a fresh test-and-deploy run for the merged commit.

| Workflow | Trigger | What it does | Result |
| --- | --- | --- | --- |
| [SmartJob CI](.github/workflows/ci.yml) | Pull request or push to `main`/`develop`, or manual run | Restores the .NET 10 solution, verifies analyzer findings at error severity, builds Release and runs the `VertecoSmartJob.Tests` project. | Fast build and test check for the web/API solution |
| [SmartJob All Tests](.github/workflows/unit-tests.yml) | Pull request or push to `main`/`develop`, or manual run | Runs `dotnet test` on the whole solution with no test filter, collects TRX results and XPlat Code Coverage, and uploads results even if tests fail. | Test and coverage artifact retained for 14 days |
| [SmartJob Main Tests](.github/workflows/main-tests.yml) | Called by Main Deploy, or manual run | Restores the solution, verifies analyzers, builds Release and runs the `VertecoSmartJob.Tests` project. | Fresh deployment gate for the merged commit |
| [SmartJob Main Deploy](.github/workflows/main-deploy.yml) | Push to `main` | Calls Main Tests, publishes API and web, deploys API to Azure, checks API readiness, deploys web and checks web health. | Hosted API and manager web app updated only after the test job succeeds |
| [SmartJob Android Build](.github/workflows/android.yml) | Pull request or push to `main`/`develop`, or manual run | Installs the MAUI Android workload and builds an unsigned PR APK, or reconstructs and validates the signing keystore before building a signed APK on push/manual runs. | APK artifact retained for 14 days |

### What happens when a change reaches `main`

1. A contributor opens a pull request. CI, All Tests and the Android build run against the proposed change. The Android PR build is unsigned and does not use the signing secrets.
2. After the required review and approval, the pull request is merged under that repository's branch rules. A resulting push to `main` in this final-project repository starts Main Deploy.
3. Main Deploy calls Main Tests first. It restores the solution, checks analyzer errors, builds Release and runs the test project again on the merged commit. A failure stops the deploy job.
4. The deploy job publishes the API and web projects separately. It uses `AZURE_API_PUBLISH_PROFILE` and `AZURE_WEB_PUBLISH_PROFILE` from GitHub Secrets to deploy to their Azure App Services.
5. The workflow polls the API `/health/ready` endpoint before deploying the web app. It then polls the web `/health/live` endpoint. A failed health check fails the workflow; it is not an automatic rollback.
6. The Android workflow also runs for the `main` push. It produces a signed APK as a downloadable Actions artifact. Publishing that APK as a GitHub Release is a separate release step.

Main Tests runs the current `VertecoSmartJob.Tests` project. All Tests runs every test project in the solution, so it will also include any additional test projects added later. The deployment workflow uses a concurrency group to prevent overlapping `main` deploys.

Android signing uses `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` and `ANDROID_KEY_PASSWORD` from GitHub Secrets. Azure publish profiles use the two Azure secrets above. Database, Blob, SMTP and Gemini runtime settings are configured for the hosted apps separately; they are not embedded in the workflow files or Android artifact.

## Contents

- [Project links](#project-links)
- [Demo sign-in](#demo-sign-in)
- [Group members and roles](#group-members-and-roles)
- [CI/CD and deployment](#cicd-and-deployment)
- [Person 1 - Backend, API and Reporting](#person-1)
- [Person 2 - Web Front End](#person-2)
- [Person 3 - Security, Identity and Notifications](#person-3)
- [Person 4 - Mobile Backend, API and Sync](#person-4)
- [Person 5 - Mobile Front End](#person-5)
- [Person 6 - Mobile Security, DevOps and Hosting](#person-6)

---

<a id="person-1"></a>
## Person 1 (Alex) - Backend, API and Reporting

This folder is the backend, API and reporting starting point for the group project. It contains Domain, Application, Infrastructure, API, and Contracts projects so the web and mobile teams can share DTOs.

All jobs use one **Standard Job Card** and its six checklist items. Clients do not choose or edit a template.

### What's included

- Guarded job card lifecycle: Draft → Assigned → Accepted → InProgress → AwaitingApproval → Completed, with Reopened and Cancelled branches.
- Submission validation for required fields, evidence, and both signatures; SHA-256 revision snapshots linked across resubmissions.
- Customer, site, user, and technician management services, soft deletion, and audit records.
- Flagged jobs and persisted corrective actions with manager resolution and audit records.
- Azure SQL EF Core provider, seed data, migrations, and an idempotent SQL deployment script.
- Private Azure Blob Storage adapter, technician job routes, scoped evidence and signature uploads, and offline mobile sync. See the [mobile API Postman collection](postman/VertecoSmartJob.Mobile.postman_collection.json) for request examples.
- `/api/v1` manager endpoints, dashboard aggregates, monthly reports with Gemini AI analysis, CSV/PDF exports, Swagger, and a Postman collection.

### Open the solution

Install the .NET 10 SDK and open `VertecoSmartJob.slnx` in Visual Studio. To run the automated .NET tests from this folder:

```powershell
dotnet restore VertecoSmartJob.slnx
dotnet build VertecoSmartJob.slnx
dotnet test tests/VertecoSmartJob.Tests/VertecoSmartJob.Tests.csproj
```

The automated tests use an isolated in-memory SQLite database. The running API uses Azure SQL.

### Set up Azure for local development

#### Save local settings in Visual Studio User Secrets

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

   `Jwt` and `Smtp` are Person 3's (Moe's) addition - see that section below for what they're for and how to generate them. Every developer running the Api locally needs their own values for all of these; none of them are committed to the repo.
3. Save `secrets.json`, set **VertecoSmartJob.Api** as the startup project, select its **http** launch profile, and press **F5**. The API uses `http://localhost:5196`; Swagger is at `http://localhost:5196/swagger`. The **https** profile also exposes `https://localhost:7086`. The launch profiles set `ASPNETCORE_ENVIRONMENT=Development`, which makes User Secrets available.


User Secrets live outside the repository and everyone configures their own local file. They are intended for development and are not encrypted. Never put real passwords, storage keys, or API keys in `appsettings.json`, the README, the Postman collection, or a commit.

Development startup applies pending Azure SQL EF migrations. The `AddStandardJobCardTemplate` migration adds only the single required template and its six checklist items when no active standard template exists. Startup does **not** insert accounts or demo records. The existing Manager account and password set through Person 3's login flow remain in the database; the API never recreates them. Automated tests use a separate in-memory SQLite database and test-only fixtures. The database must be reachable before the API can start. If the SQL firewall or credentials are wrong, startup stops during migration. The Api now authenticates requests through **either** the development API-key header **or** a real JWT Bearer token - see Person 3's section below for how real login, password reset, and role enforcement work.

For deployment, use the reviewed `Documents/azure-sql-migration.sql` or EF migrations instead of having every production API instance migrate at startup.

The idempotent deployment script now includes Person 3's `AddUserIdentityFields` and `AddPasswordResetTokens` migrations. It prepares the schema only; it does not enable Production hosting or replace the required deployment configuration review.

After updating the code, restart the **API** in Visual Studio to apply the new migration. `GET /api/v1/jobtemplates/standard` should then return **200** with exactly six checklist items. It adds no demo customers, technicians, users, or jobs; those lists stay empty until records are created. If the template route still returns **404**, confirm the API restarted with the updated build and that `20261003120000_AddStandardJobCardTemplate` appears in `__EFMigrationsHistory`.

### Gemini report analysis and PDF downloads

Gemini AI generates the management analysis by default. `Ai:Provider` is `Gemini` and `Ai:Model` is `gemini-3.5-flash-lite` in the API settings. The prompt asks for a monthly overview, workflow bottlenecks, risk and priority, two numbered management actions, and data limitations. It uses created/completed counts, average completion time, current status breakdown, flagged jobs, and high or urgent jobs. The prompt prevents a misleading completion rate because jobs created and completed in the same month can be different groups.

If Gemini has no key, is rate limited, or cannot respond, the report returns an explicit `aiStatus` with no AI commentary. The statistics and exports remain available. `Ai:Provider=RuleBased` is an optional offline mode that generates labelled local insights; it is not AI-generated analysis.

The PDF is generated by the API itself and includes the same report insights shown on the report page. CSV contains the numeric statistics. Report downloads remain available when an external AI provider is unavailable.

### Use the Postman collection

The collection is `postman/VertecoSmartJob.Person1.postman_collection.json`. It contains **38 request examples**. It has no Postman assertion scripts, so a Runner result of **"0 tests"** means no automated assertions were defined; it does not indicate whether the HTTP responses were correct. Run the requests individually in the order below. Running the entire collection at once will encounter empty IDs, invalid job states, and requests that deliberately change data.

#### 1. Import and configure

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

#### Authenticate in Swagger (Development)

Open `http://localhost:5196/swagger` and click **Authorize**. For **SmartJobKey**, enter the development key value only; Swagger sends it as `X-SmartJob-Key`. For **Bearer**, first call `POST /api/v1/auth/login`, copy `accessToken` from the response, and enter only that token; Swagger adds the `Bearer` prefix. Authorize with one scheme at a time. If both are filled in, the API selects Bearer whenever an `Authorization` header is present. Click **Logout** in the Authorize dialog before switching schemes. Swagger is available only in Development.

#### 2. Run the read-only requests

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
| **Monthly report and insights** | **200** for valid `reportPeriod`; check `aiStatus` and `aiCommentary`. Gemini is the default. A missing key or provider error leaves the statistics available and reports the AI status. |
| **Export report CSV** | **200**, `text/csv`; use **Send and Download** in Postman to save the file. |
| **Export report PDF** | **200**, `application/pdf`; use **Send and Download** to save the file. It includes report insights. |
| **Audit trail** | **200**; repeat after writes to see new entries. Now also includes Person 3's auth events (`LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `PasswordResetRequested`, `PasswordReset`, `PasswordSetByManager`) alongside Alex's job-lifecycle entries, in the same table. |
| **List flagged jobs** | **200**; `items` contains only jobs with `isFlagged: true`. |
| **List corrective actions** | **200**; defaults to `status=open`. Copy an `items[].id` into `correctiveActionId`; change `status` to `resolved` to view history. |
| **Get corrective action** | **200** for a valid `correctiveActionId`; shows the reason, job number, and resolution fields. |

#### 3. Test customer, site, and job writes in order

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
| 7 | **Assign technician** | **200** after a job and technician exist. Use **Create technician and account** and copy its `id` to `technicianId`. Once assigned, the draft edit request correctly returns **409 Conflict**. Assigning now also sends the technician an email notification (Person 3) if SMTP is configured - see below. |

The collection's **Approve submitted job** request cannot approve a newly assigned job. A technician must accept it, start it, complete its checklist, add evidence and both signatures, then submit it for review. Use the technician routes and the [mobile API Postman collection](postman/VertecoSmartJob.Mobile.postman_collection.json) to complete that workflow. The Person 1 collection still covers manager requests.

Next, use that completed job to send **Reopen job**. Expect **200** and status `Reopened`. To test **Cancel active job**, first set `jobId` to a *different* job in `Draft`, `Assigned`, `Accepted`, `InProgress`, `AwaitingApproval`, or `Reopened`; expect **200** and `Cancelled`. Cancelling a `Completed` or already `Cancelled` job correctly returns **409**. These requests change shared Azure data, so choose disposable records when possible.

#### 4. Test user management safely

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


#### 5. Test technician creation and corrective actions

**Create technician and account** sends `POST /api/v1/technicians` as a Manager. Change the sample email before repeating it. Expect **201 Created** with both `id` (the new `technicianId`) and `userId` (the linked account for `/api/v1/users/{id}` suspend/reactivate/deactivate routes). The Technician and User are saved together. **Do not** call `POST /api/v1/users` again for this technician. Set the collection's `technicianId` to the returned `id`, then send **Edit technician** (`PATCH /api/v1/technicians/{{technicianId}}`); expect **200** and verify skills/availability. Name, email, and phone edits also update the linked User. The new endpoints require a Manager key.

For a flagged-work walkthrough, set `jobId` to an existing job that is not Cancelled. Send **Flag job** with a nonempty `reason`; expect **200** with `isFlagged: true`, `flagReason`, and one new entry in `correctiveActions`. Managers can flag a job; an authenticated Technician may flag only a job assigned to them. **List flagged jobs** (`GET /api/v1/jobs?flagged=true`) should now contain this job. **List corrective actions** with `status=open` should contain its action. Copy that action's `id` into `correctiveActionId`, then send **Get corrective action** to inspect it.

Resolve the flag using **either** **Resolve corrective action** (`POST /api/v1/corrective-actions/{{correctiveActionId}}/resolve`) **or** **Resolve job flag** (`POST /api/v1/jobs/{{jobId}}/resolve-flag`). Both take a nonempty `resolutionNotes` body and require a Manager key. Expect **200**; the job's `isFlagged` becomes `false`, and the corrective action remains in history with its resolution. Change **List corrective actions** to `status=resolved` to see it. Resolving the same action twice returns **409 Conflict**. A job in `AwaitingApproval` cannot be approved while it has an open flag; resolve it first.

Each flag creates one corrective-action record with a reason, open/resolved state, timestamps, actor IDs, and resolution notes. This is the minimal persisted workflow for Person 2's Flagged Items and Corrective Actions screens; it does not yet include independent tasks, assignees, or due dates. A new EF migration adds these fields and records. The API applies it on the next Development startup; deployed databases need the updated migration script.

#### Troubleshooting Postman and Azure

| Symptom | Check |
| --- | --- |
| API stops at `MigrateAsync`, SQL host not found | Use the exact `*.database.windows.net` server name in User Secrets; check internet/DNS and Azure SQL Networking. |
| SQL login or firewall error | Check SQL login/password, database name, your current public IP firewall rule, and whether the free database has paused after its allowance. |
| API says `ConnectionStrings:AzureSql` or `Blob:ServiceUri or Blob:ConnectionString` is required | Open **Manage User Secrets** on the **API project**, verify the JSON keys and run with the **Development** launch profile. |
| Postman `401` | Confirm the API is running and `managerKey` exactly matches User Secrets; the email must identify an existing, active user. If testing with a Bearer token instead, confirm the token hasn't expired (30-minute lifetime) and that `Authorization: Bearer <token>` is actually present on the request - Postman's Authorization tab can silently fail to apply if a conflicting header is also set manually; check the Headers tab directly if in doubt. |
| Web dashboard redirects to login after its API token expires | Sign in again with the Manager account. The Web cookie can outlive its API token; the Web app now clears an invalid cookie when the API returns `401`. If a fresh login still fails, check the API's `JwtBearer` authentication warning and verify `Api:BaseUrl` points to the API's HTTPS address. |
| Postman `403` | The key or token identified a user without permission for that manager endpoint - this is expected and correct for a Technician token hitting a Manager-only route. |
| Postman `400` | Check required body fields, valid GUID variables, JSON syntax, and the `yyyy-MM-dd` job date or `yyyy-MM` report period. |
| Postman `404` or `405` on an ID request | Fill `jobId`, `customerId`, `siteId`, or `userId`; an empty variable can turn a detail URL into a collection URL. |
| Postman `409` | Check the job's current `status` and flag state; edits require `Draft`, approval requires `AwaitingApproval` with no open flag, and a flag can be resolved only once. |
| Postman `423` | The account is locked out after 5 failed login attempts; wait 15 minutes or have a Manager reset it via `set-password`. This is correct, intended behaviour - see Person 3's section. |
| Postman Runner says **0 tests** | The collection has request examples but no Postman assertion scripts. Inspect response status/body for each request using the tables above. |

---

<a id="person-2"></a>
## Person 2 (Caleb) - Web Front End setup

This section documents the implemented `VertecoSmartJob.Web` manager console and its integration with the shared API.

### What's included

A server-rendered ASP.NET Core MVC front end, calling this API through typed `HttpClient`s rather than any JavaScript framework. Screens built so far:

- **Dashboard** - summary stats (active job cards, due today, technicians available).
- **Job Cards** - list/filter, create (with the one seeded Standard checklist attached automatically), and a details view with the lifecycle tracker (Draft → Assigned → Accepted → InProgress → AwaitingApproval → Completed, plus Reopened/Cancelled).
- **Customers** - list, details (with its sites), create, edit; create/edit also support creating a new site inline against that customer.
- **Sites** - list, details (with equipment), create, edit.
- **Technicians** - list, details (with linked user account status and Suspend/Reactivate/Deactivate actions), create, edit.
- **Flagged Items** - list of flagged jobs and their open corrective actions (Manager only).
- **Historical Records** - searchable record view across jobs/equipment history.
- **Reports** - monthly report list and a details page with stats, status breakdown, report insights, and CSV/PDF export links (Manager only).
- **Activity Feed** - audit trail view (Manager only).
- **Profile & Settings** - signed-in user details, editable name/email/phone, password reset and logout.
- **Login** - real email/password sign-in against the Api's `/api/v1/auth/login` endpoint. See Person 3's section below for how accounts get their first password and how the identity system works end to end.
- **Privacy Notice** - a POPIA-aligned privacy page (Person 3), linked from the sidebar under Account.

Manager-only pages and nav items are hidden from Technician accounts via `User.IsInRole("Manager")` checks, matching the API's own role rules.

### Project structure - how to navigate

Everything below lives inside `src/VertecoSmartJob.Web/`:

| Folder / file | What's in it |
| --- | --- |
| `Program.cs` | App startup - registers one typed `HttpClient` per API area, cookie auth setup, MVC routing, and (Person 3) a security response headers middleware. |
| `Auth/` | `ApiKeyDelegatingHandler.cs` - attaches the signed-in user's JWT as a `Bearer` token to every outgoing API request (Person 3; the file name is unchanged from the original API-key version, but it now carries a real token read from the sign-in cookie rather than a static shared key). |
| `Api/` | One typed client class per API area (`JobCardsApiClient`, `CustomersApiClient`, `SitesApiClient`, `TechniciansApiClient`, `UsersApiClient`, `DashboardApiClient`, `ReportsApiClient`, `AuditApiClient`, `CorrectiveActionsApiClient`, `JobTemplatesApiClient`), plus `AuthApiClient.cs` (Person 3 - calls `/api/v1/auth/login`; deliberately does **not** use `ApiKeyDelegatingHandler`, since logging in is the one call made before any credential exists), and shared plumbing (`ApiException.cs`, `ApiResponseExtensions.cs`, `AuditEntryDto.cs`, `JobCardListQuery.cs`). Controllers never call the API directly - they go through these. |
| `Models/` | View models for forms and list/detail pages (one per screen, e.g. `JobCardListViewModel.cs`, `TechnicianDetailsViewModel.cs`, `CustomerFormViewModel.cs`, `LoginViewModel.cs`). |
| `Controllers/` | One controller per feature area - `HomeController` (Dashboard), `JobCardsController`, `CustomersController`, `SitesController`, `TechniciansController`, `FlaggedItemsController`, `HistoryController`, `ReportsController`, `ActivityController`, `ProfileController`, `AccountController` (real login/logout, Person 3), `PrivacyController` (Person 3). |
| `Views/{ControllerName}/` | The Razor pages for that controller - folder names match controller names exactly (e.g. `Views/Technicians/` holds `Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`). |
| `Views/Shared/` | `_Layout.cshtml` (the sidebar/topbar shell every page renders inside) and `Error.cshtml`. |
| `Views/_ViewStart.cshtml`, `Views/_ViewImports.cshtml` | Wires every view to `_Layout.cshtml` and brings in shared `@using`s and tag helpers. |
| `wwwroot/css/site.css` | All styling - colors, layout, the login screen, sidebar, cards, tables, etc. |
| `wwwroot/js/site.js` | Sidebar toggle, toast notifications, and the inline customer/site creation panels on the job card form. |
| `wwwroot/images/` | `verteco-logo-full.png` and `verteco-mark-transparent.png`. |
| `appsettings.json` | Non-secret config - `Api:BaseUrl` only. (The old `DevAccounts` dev-login placeholder has been removed - see Person 3's section.) |

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

`VertecoSmartJob.Web` lives directly inside `src/`, as a sibling of `VertecoSmartJob.Api`, `VertecoSmartJob.Application`, `VertecoSmartJob.Contracts`, `VertecoSmartJob.Domain`, and `VertecoSmartJob.Infrastructure` - not nested inside an extra `src/src/...` folder, and not at the solution root. Its `.csproj` has exactly one project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\VertecoSmartJob.Contracts\VertecoSmartJob.Contracts.csproj" />
</ItemGroup>
```

A duplicated/incorrect second `ProjectReference` line (pointing at `..\src\VertecoSmartJob.Contracts\...`) will cause `NU1104`/`MSB9008` build errors and a cascade of unrelated-looking `CS1061` errors in the Views - if you see that, check this file first.

Separately: `VertecoSmartJob.slnx` should list every project **once**, each under the correct folder (e.g. `src/VertecoSmartJob.Web/VertecoSmartJob.Web.csproj`). A stray duplicate entry with the wrong path (missing the `src/` prefix) has shown up before and causes Visual Studio to display a second, broken "(not found)" copy of a project in Solution Explorer alongside the real one. If you see this, open `VertecoSmartJob.slnx` in a text editor and remove the incorrectly-pathed duplicate line - do not remove the correct one.

### Running the Web app against the Api

- `Web/appsettings.json` points `Api:BaseUrl` at `https://localhost:7086` (the Api's **https** profile - see the note below on why https specifically matters).
- To run both projects together from one Visual Studio instance: right-click the solution → **Configure Startup Projects…** → **Multiple startup projects** → set both `VertecoSmartJob.Api` and `VertecoSmartJob.Web` to **Start**, then F5.
- Person 3's note: point `Api:BaseUrl` at the Api's **https** URL, not http. The Api redirects http → https (`UseHttpsRedirection`), and .NET's `HttpClient` strips the `Authorization` header when a request is redirected across a scheme/port boundary, as a security safeguard. Pointing at http will cause every authenticated request from the Web app to silently lose its Bearer token mid-redirect, which looks like a confusing, intermittent `401` rather than an obvious configuration error.
- If you're only running Web locally against an Api someone else is hosting, you no longer need any shared secret between the two for login itself - the Web app simply calls that Api's real `/api/v1/auth/login` endpoint and uses whatever token comes back. You do still need the Api's database/storage secrets if you're running the Api yourself rather than pointing at someone else's running instance.

### Static assets

`wwwroot/css/site.css` and the two logo images in `wwwroot/images/` (`verteco-logo-full.png`, `verteco-mark-transparent.png`) must exist in the Web project for the UI to render with its intended styling. These are plain static files, not generated at build time - if the site loads with no styling or broken image icons, check these are actually present first.

### Known local setup issue: Azure SQL firewall

If the Api throws something like:

```
Microsoft.Data.SqlClient.SqlException: Cannot open server 'smartjob' requested by the login.
Client with IP address 'x.x.x.x' is not allowed to access the server.
```

this is Azure SQL's firewall rejecting your public IP - not a code or configuration problem on the Web side. Whoever owns the Azure SQL server needs to add that IP under **Azure Portal → SQL servers → `smartjob` → Networking → Firewall rules**, which can take a few minutes to take effect. Since the Api won't start at all while this is unresolved, any Web page that depends on real data will hang or fail until it's fixed. Home/mobile IPs can change over time, so this may need to be re-added later.

---

<a id="person-3"></a>
## Person 3 (Moe) - Security, Identity & Notifications

This section documents the real authentication system, password reset flow, audit/notification wiring, and security hardening built on top of Persons 1 and 2's work - replacing the temporary dev-account login bridge that the Web app originally shipped with.

### What's included

**Identity core**
- `User` (Domain) extended with `PasswordHash`, `IsEmailConfirmed`, `FailedLoginAttempts`, and `LockedOutUntilUtc` - all behind behavior methods (`SetPasswordHash`, `ConfirmEmail`, `RegisterFailedLogin`, `ResetFailedLogins`, `IsCurrentlyLockedOut`), never public setters.
- `PasswordHashingService` (Infrastructure) - PBKDF2 hashing via ASP.NET Core's built-in `PasswordHasher<User>`. Salted automatically; the same password hashed twice never produces the same stored value.
- `JwtTokenService` (Infrastructure) - issues signed, short-lived (30 minute) access tokens carrying `ClaimTypes.NameIdentifier`, `Name`, `Email`, `Role`, and a `TechnicianId` claim when applicable. This is deliberately the **same claim shape** Alex's `ApiKeyAuthenticationHandler` already produces, so every existing `[Authorize(Roles=...)]` check and `ActorTechnicianId`/`ICurrentUser` lookup across the codebase works unchanged regardless of which credential type authenticated the request.
- `AuthController` (`/api/v1/auth`): `POST /login`, `POST /forgot-password`, `POST /reset-password`, and `POST /users/{id}/set-password` (Manager-only).
- Dual authentication in the Api's `Program.cs`: a policy scheme forwards to JWT Bearer validation when a request carries an `Authorization` header, or to Alex's existing API-key scheme otherwise. Both are fully supported side by side - Postman's `X-SmartJob-Key` testing flow still works exactly as documented above, and the Web app now uses the JWT path.

**Password reset, with real email**
- `PasswordResetToken` (Domain) - a single-use, time-limited (1 hour) token. The emailed link contains the raw token; only its SHA-256 hash is ever stored, same principle as the password itself.
- `IEmailSender` / `SmtpEmailSender` (Infrastructure) - sends real email via Gmail SMTP. Failures are logged, not thrown, so a flaky mail server never blocks the calling request.
- `POST /api/v1/auth/forgot-password` always returns the same response whether or not the email exists, to avoid leaking which emails are registered. `POST /api/v1/auth/reset-password` validates the token, updates the password, and marks the token used.
- This has been tested end to end: a real password reset email was sent and received, and the returned link's token successfully completed a password change.

**Lockout and rate limiting**
- 5 failed login attempts locks the account for 15 minutes (`423 Locked`), even if the correct password is then supplied during that window.
- A dedicated `"auth"` rate-limit policy (10 requests / 5 minutes) applies to `login`, `forgot-password`, `reset-password`, and `set-password`.
- This has been tested directly: 5 wrong-password attempts in a row produced a real `423` on the 6th, confirmed in the audit log.

**Audit trail and notifications**
- Auth events (`LoginSucceeded`, `LoginFailed`, `AccountLockedOut`, `PasswordResetRequested`, `PasswordReset`, `PasswordSetByManager`) write into the same `AuditLog` table Alex's job-lifecycle code already uses, via `IAuditLogRepository` - one unified audit trail, not two separate logs.
- `INotificationService` / `EmailNotificationService` (wired into `JobCardService`) sends a real email when a job is assigned, submitted for review, approved, reopened, or flagged. Technicians are emailed directly (via their linked `User.Email`); Managers are emailed as a group for events needing their review. This has been tested by assigning a real job and confirming no send failure was logged.

**Web app integration**
- `AccountController` now calls the real `/api/v1/auth/login` endpoint via a new `AuthApiClient`, storing the returned JWT in the sign-in cookie instead of the old static API key. The old `DevAccount.cs` model and the `DevAccounts` config sections have been removed - they're no longer referenced anywhere.
- `ApiKeyDelegatingHandler` (same file name, different contents) now attaches `Authorization: Bearer <token>` from the signed-in cookie to every outgoing API call, instead of a static `X-SmartJob-Key`.

**Hardening**
- CSRF: every mutating Web controller action already had `[ValidateAntiForgeryToken]` (confirmed by checking all 12 controllers - every `[HttpPost]` has a matching token check).
- SQL injection: confirmed no raw SQL anywhere in the codebase; everything goes through EF Core's parameterized queries.
- Security response headers added to both apps: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin` on both the Api and Web app, plus a `Content-Security-Policy` on the Web app specifically (the Api is a pure JSON API and doesn't render HTML, so CSP doesn't meaningfully apply there except to its dev-only Swagger UI).
  - The Web app's CSP allows `'unsafe-inline'` for scripts and styles. This is a deliberate, documented trade-off: the app already uses inline `<script>` (on the Create Job Card page), inline `onclick=` handlers, and inline `style=""` attributes across many views. A stricter, nonce-based CSP is the textbook-correct approach but would require refactoring those into external files - flagged here as a known limitation rather than hidden.
- A `JwtBearer` authentication-failure logger was added (`OnAuthenticationFailed` in `Program.cs`) so a rejected token now produces a diagnostic log line instead of a silent `401` with no explanation.
- Found and fixed a real authorization bug along the way: `TechniciansController.Suspend` (Web) was missing `[Authorize(Roles = "Manager")]`, unlike its two neighboring actions (`Reactivate`, `Deactivate`). The Api's own `UsersController` still had a class-level Manager-only guard, so this was never actually exploitable end to end, but it's a good example of defense-in-depth catching a gap in one layer.

**POPIA / privacy**
- A **Privacy Notice** page (`/Privacy`, `PrivacyController` + `Views/Privacy/Index.cshtml`) covering what's stored, why (including the client-confirmed indefinite retention policy, for warranty claims and renewals), and how it's protected. Linked from the sidebar under Account. Anonymous-accessible by design, since a privacy notice should be readable without needing to log in first.

**In-app notifications (mobile)**
- The mobile Notifications screen was previously a static placeholder with hardcoded "No new notifications" text and no real data behind it. It's now backed by a real `Notification` entity (Domain), persisted by `EmailNotificationService` alongside every email it already sends - so each job assigned/submitted/approved/reopened/flagged event now produces both a real email **and** a persisted in-app notification for the relevant user.
- `MobileNotificationsController` (`/api/v1/mobile/notifications`, Technician-only) exposes `GET` (paged, for the signed-in technician only) and `POST /{id}/read` (marks one read, with an ownership check so a technician can't mark someone else's notification read by guessing a GUID).
- On mobile, `NotificationsViewModel` and the rewired `NotificationsPage.xaml` replace the old static page with a real `CollectionView` bound to live data, keeping the original empty-state visual (the bell icon) as the `CollectionView`'s `EmptyView` rather than a separate always-shown block.
- **Migration note:** the `AddNotifications` EF migration is in the codebase, while the checked-in `Documents/azure-sql-migration.sql` does not include that migration. The API applies pending EF migrations at startup when its database is reachable.

### How to actually log in for the first time

The existing Manager login has a working password. Any new account has no password until a Manager sets one:

1. Run the Api and Postman's **List users** to find the target user's `id` (a `User` id, not a `Technician` id - they're different GUIDs for the same person).
2. As a Manager (via `managerKey` or a Manager's own JWT), call:
   ```
   POST {{baseUrl}}/api/v1/auth/users/{id}/set-password
   { "newPassword": "SomeStrongPassword123!" }
   ```
   Expect `204 No Content`. This also auto-confirms the email and clears any lockout state on that account.
3. That person can now log in for real at `/Account/Login` (Web) or `POST /api/v1/auth/login` (API directly), using that email and password.
4. From then on, they can use `POST /api/v1/auth/forgot-password` to reset it themselves via a real emailed link, without needing a Manager's help again.

### Gmail SMTP setup (for anyone who needs to run email-dependent features locally)

1. Use a dedicated Gmail account for this project (not a personal one) - this is what sends every notification and reset email, and the email will visibly come from this address.
2. Enable **2-Step Verification** on that Google account (Security settings).
3. Generate an **App Password** at `myaccount.google.com/apppasswords` - this 16-character value, not the normal Gmail password, goes in `Smtp:Password` in User Secrets.
4. Without this configured, email-dependent features (reset, notifications) don't throw - they log a warning and continue, so the rest of the app still works, but no real email goes out.

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
- **XSS:** `<script>alert('xss')</script>` entered into a job's notes field rendered as literal encoded text in the page's actual HTML source, not as an executable script - confirmed via View Page Source, not just the browser's rendered view.

---

<a id="person-4"></a>
## Person 4 (Isaac) - Mobile Backend, API & Sync

| | |
| --- | --- |
| **Role** | Mobile Backend, API & Sync Lead |
| **Development branch** | `Isaac-Mobile-Backend-API-and-Sync` |
| **Latest sync commit** | `b71e8e7` - Add mobile delta pull sync |
| **Verification** | 31 tests passed, 0 failed |

This section records the mobile-facing backend and offline synchronization implementation.

### Role overview

Responsible for the mobile-facing backend, API, and synchronization layer - giving the mobile application a secure backend through which technicians can access assigned jobs, perform field-work actions, capture job information, and continue working when connectivity is weak or unavailable. This work was kept on a dedicated branch so it could be developed and tested without pushing directly to `main` or overwriting another team member's work.

### Main work completed

- Mobile job API endpoints for technician field work
- Job accept and start workflow
- Checklist response updates
- Equipment and serial information capture
- Evidence upload instruction and confirmation workflow
- Technician and client signature confirmation
- Job submission support
- Offline push synchronization for queued mobile operations
- Idempotency handling to prevent duplicate processing
- RowVersion-based conflict detection
- Checklist conflict handling and non-overlapping update support
- Delta pull synchronization for reconnecting mobile devices
- Entity Framework persistence and migrations for mobile synchronization
- Automated build and test verification

### Mobile API

The mobile API is implemented under the versioned mobile route:

```
/api/v1/mobile/
```

Technician identity is obtained from authentication claims rather than accepting an arbitrary technician ID from the client. This supports ownership checks and helps prevent one technician from requesting another technician's jobs.

**Job operations** - the mobile job controller supports the core field-work workflow:

- Retrieve jobs assigned to the authenticated technician
- Retrieve individual job details with access checks
- Accept an assigned job
- Start an accepted or reopened job
- Update checklist responses during an in-progress job
- Capture equipment/serial information
- Request and confirm evidence uploads
- Confirm technician and client signatures
- Submit completed field work for review

### Evidence upload workflow

Evidence support was added for mobile field work. The file store restricts supported evidence to JPG, PNG, and PDF files, with a maximum file size of 10 MB. The mobile client requests temporary upload instructions, uploads the file, and then confirms the upload with the backend:

1. The mobile app requests upload instructions from the API.
2. The backend creates a short-lived upload instruction/token.
3. The mobile app uploads the evidence file.
4. The app confirms the upload.
5. The backend checks the uploaded object and returns/stores the evidence reference for the job.

The confirmation logic checks that the uploaded object exists and validates relevant information such as extension, size, content type (where available), and SHA-256 checksum.

### Signatures and submission

The mobile backend supports both Technician and Client signature types. Signature information is associated with the job, and the existing submission validation requires the required sign-off before the job can proceed through the submission workflow.

Job submission also uses the existing revision/sealing mechanism. A revision is created when field work is submitted for review, protecting the submitted state from silent modification.

### Equipment and serial capture

Equipment capture was added to the mobile backend so technicians can record equipment information required during field work. This is exposed through the mobile API and persisted through the application/repository layer.

### Offline synchronization

Offline synchronization is a major part of this implementation. The mobile application can continue collecting changes while disconnected and later send queued operations to the backend after reconnecting.

**Sync endpoints:**

```
GET  /api/v1/mobile/sync/pull
POST /api/v1/mobile/sync/push
```

The pull endpoint supplies server-side job data to the mobile application. The push endpoint processes operations that were queued while the device was offline.

**Offline push:** each queued operation includes an operation identifier and the relevant job/version information. The backend processes supported operations and returns a result for each item, so the mobile client can determine whether it was applied, replayed, or rejected because of a conflict.

**Idempotency:** `MobileSyncOperation` records are used to remember operations that have already been processed, preventing a retry from applying the same offline operation twice. A unique database index is used for the technician and operation identifier combination.

**RowVersion conflict detection:** entities use `RowVersion` values as concurrency tokens. The mobile device sends the version it originally worked from, and the backend compares that against the current server version before applying offline changes.

- Matching versions allow the update to proceed.
- Checklist item versions allow item-level conflict checking.
- Non-overlapping checklist changes can be applied without overwriting unrelated work.
- A conflicting update is returned as a conflict instead of silently overwriting the server state.

### Delta pull / reconnect sync

Rather than returning every assigned job on every reconnect, the backend can return only jobs that changed after the client's previous successful pull.

A `MobileSyncChange` entity and persistence configuration were added. `AppDbContext` records a sync change whenever a `JobCard` or `JobChecklistItem` is added or modified. The repository can then query those change records for a technician within the requested synchronization window.

The pull response contains `ServerTimeUtc`. The mobile application stores this value and uses it as the `since` value on its next pull. The server captures an upper synchronization boundary before reading changes, so the response has a consistent cutoff:

```
GET /api/v1/mobile/sync/pull?since=<previous ServerTimeUtc>
```

### Database and Entity Framework work

This implementation added persistence support for mobile synchronization. The main synchronization entities are `MobileSyncOperation` (processed offline operations) and `MobileSyncChange` (delta-pull change tracking):

- `MobileSyncOperation` entity and EF configuration
- `MobileSyncChange` entity and EF configuration
- `DbSet` registrations in `AppDbContext`
- `RowVersion` refresh for added or modified entities
- Automatic mobile change records for `JobCard` and `JobChecklistItem` changes
- Repository support for full initial pull and filtered delta pull

Relevant migrations:

```
MobileSyncOperations
MobileSyncDeltaPull
```

### Security and access control

Mobile controllers are restricted to the Technician role. The authenticated technician ID is read from the user's claims and used for job access and synchronization - mobile job access checks mean the API does not simply trust an ID supplied by the client.

Evidence upload instructions are temporary rather than giving the mobile application unrestricted storage credentials.

### Build and testing

The solution was repeatedly built during development. After the delta-pull migration was generated, the complete available test suite was run successfully:

| Total | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| 31 | 31 | 0 | 0 |

The existing tests include coverage for the implemented mobile backend areas such as synchronization conflict behavior and equipment capture. Passing the suite confirms that the committed changes compile and do not break the current automated tests.

### Main areas of the solution

| Location | Purpose |
| --- | --- |
| `src/VertecoSmartJob.Api/Controllers/Mobile/` | Mobile-facing API controllers, including job and sync endpoints. |
| `src/VertecoSmartJob.Application/Jobs/` | Application services, including `MobileSyncService`. |
| `src/VertecoSmartJob.Application/Abstractions/` | Repository/service abstractions used by the mobile backend. |
| `src/VertecoSmartJob.Domain/Entities/` | Domain entities, including mobile synchronization tracking entities. |
| `src/VertecoSmartJob.Infrastructure/Persistence/` | EF Core `DbContext`, repositories, configurations, and migrations. |
| `tests/VertecoSmartJob.Tests/` | Automated project tests. |


### Final status

The core deliverables implemented in this branch include the mobile job API, field-work operations, equipment capture, evidence/signature support, offline queued operation processing, idempotency, RowVersion conflict detection, checklist conflict handling, and reconnect delta synchronization.

At the final verification point on this contribution branch, the solution built successfully and all 31 tests then available passed. The integrated repository's current test and deployment checks are described in the CI/CD section above.

---

<a id="person-5"></a>
## Person 5 (Erin) - Mobile Front End


### Overview

SmartJob Technician Mobile is a .NET MAUI application developed as part of the Verteco SmartJob system, for technicians completing fieldwork. It provides a structured workflow for managing assigned jobs from initial review through to final submission.

### Technology stack

- .NET MAUI
- C#
- XAML
- MVVM architecture
- SQLite for local data storage
- ASP.NET Core Web API integration

### Mobile application

The application provides technicians with a complete field workflow: accessing assigned jobs, reviewing job information, completing required checks, recording field information, and submitting completed work for approval.

The final implementation includes job status management, equipment recording, photographic evidence, issue reporting, digital signatures, final review, and offline synchronisation.

### Offline support

SmartJob Mobile was designed to support technicians working in areas with unreliable network connectivity. Job and workflow information is stored locally using SQLite, while the Offline Sync functionality shows connectivity status, pending changes, and synchronisation status. A manual **Sync Now** option is available when connectivity is present. This directly reflects the WIL checklist's requirement for offline-capable field work, and was one of the harder pieces of the original scope to get right given it depends on Person 4's conflict-detection and delta-sync work on the backend.

### Architecture

The mobile application follows the MVVM pattern to keep the user interface separated from application logic:

- **Views** - XAML pages used by the technician.
- **ViewModels** - UI state, commands, and workflow logic.
- **Models** - mobile and locally cached data structures.
- **Services** - API communication, connectivity, local SQLite storage, and synchronisation.

### API integration

The application communicates with the SmartJob backend API for job information and field operations. Mobile API functionality is provided by the Person 4 backend implementation, while this (Person 5) application is responsible for presenting and managing that information through the technician interface. Authentication and final mobile security integration, including JWT and secure token handling, form part of Person 6's integration responsibilities.

### User interface

The application maintains the SmartJob dark and red visual identity introduced during Part 1, adapting the original Figma prototype into a functional mobile interface, with clear navigation, readable job information, validation messages, system feedback, and consistent controls throughout the technician workflow. It was also tested at different window sizes to confirm the interface remains usable and responsive. This continuity from the Part 1 prototype to a working Part 2 implementation is itself part of what the role was originally scoped to deliver - carrying the agreed visual identity through rather than redesigning it mid-project.

### Validation and feedback

The application provides validation throughout the technician workflow to prevent incomplete information from progressing to final submission, with clear feedback when information is missing, when synchronisation occurs, and when a job reaches a different lifecycle stage.

### Testing

The application was tested through the complete technician workflow using jobs supplied by the SmartJob backend - covering navigation, job progression, local persistence, validation, evidence capture, digital signatures, final submission, status changes, and synchronisation. The final Android project builds successfully using .NET MAUI.

### Running the mobile application

The SmartJob backend API and required development services must be running before testing functionality that communicates with the server. Open the SmartJob Mobile project in Visual Studio, restore the required NuGet packages, and run the application using a supported .NET MAUI target.

### Project responsibilities

This repository contains work completed as part of a six-person development team. This (Person 5) contribution focuses on the SmartJob Technician Mobile front end, including the technician interface, MVVM implementation, API consumption, local persistence, offline-first user experience, responsive design, and application feedback. Backend mobile API and synchronisation functionality are coordinated with Person 4, while mobile authentication, deployment, and Android release packaging are coordinated with Person 6.


---

<a id="person-6"></a>
## Person 6 (Keeran) - Mobile Security, DevOps & Hosting

This contribution spans mobile authentication, Android/iOS release engineering, Azure hosting preparation and CI/CD. It connects Person 4's mobile API and Person 5's interface to a signed, authenticated app that can reach the hosted service.

This role focused on the security, deployment, and release side of the SmartJob mobile application.

### What was worked on

- Connected the mobile login to the real API using JWT authentication.
- Added refresh tokens so users can stay signed in securely without having to log in every time.
- Used .NET MAUI `SecureStorage` to safely store access and refresh tokens.
- Added automatic token refresh and session-expiry handling.
- Added secure logout, which clears stored tokens and revokes the refresh token.
- Removed the hard-coded API key from the mobile app and changed API requests to use Bearer tokens.
- Configured the mobile app to use the hosted Azure API in production. `ApiConfiguration.BaseUrl` points at the hosted Azure URL unconditionally. There is no Debug/Release split pointing local builds at an emulator address, so Windows, Android and iOS builds all use the hosted API unless this configuration is changed for local testing.
- Updated the Android package name to `za.co.verteco.smartjob`.
- Disabled cleartext HTTP for Android Release builds.
- Added GitHub Actions for build checks, static analysis, and automated tests.
- Added an Android build workflow that creates APK artifacts.
- Set up secure Android release signing using a keystore and GitHub Secrets.
- Built and tested a signed Android Release APK.
- Added production health checks for the API and Web application.
- Helped prepare the project for Azure hosting and production deployment.
- Set up branch protection so changes to `main` require a pull request, approval, and successful CI checks.
- Added iOS build and release documentation for the steps required on macOS.

This work connects directly across the other three mobile-adjacent roles: it takes Person 4's backend endpoints and Person 5's front-end UI and is what actually makes them reachable from a real device - hosted, signed, and authenticated - rather than only working against a developer's local machine.

### Security and deployment

Sensitive information such as signing passwords, JWT keys, and database credentials are not stored directly in the repository - these values are kept in secure configuration or GitHub Secrets. The mobile app uses HTTPS, secure token storage, and refresh-token rotation to keep user sessions secure. The CI/CD workflow checks that the project builds successfully and that automated tests pass before changes are merged.

### Mobile release

The Android version can be built as a signed APK and connects to the hosted SmartJob Azure API.

The final Android package ID is:

```
za.co.verteco.smartjob
```

The iOS version uses the same .NET MAUI project and authentication setup. The iOS build and release steps are documented in `Documents/ios-build-and-release.md`.

