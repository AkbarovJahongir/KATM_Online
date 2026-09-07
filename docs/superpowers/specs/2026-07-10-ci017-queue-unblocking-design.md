# CI-017 Queue Unblocking — Design

## Context

`CreditBureauWorker` (CreditBureauService\CreditBureauWorker.cs) polls the loan
queue every `WorkerSettings.DelayMilliseconds` (10s) via
`LoanProcessingService.CreditBureauProcessing()`. For each application it
routes to `CreditReportService` (Infrastructure\CreditReports\CreditReportService.cs)
based on `application.Status`:

- `"00"` / `"01"` / `"02"` → `CreditReport()` (submit CI-017 report request)
- `"03"` → `CreditReportStatus()` (poll CI-017 report status)

A parallel, functionally identical implementation exists for the XML pipeline:
`CreditReportXmlService` (Infrastructure\CreditReportsXml\CreditReportXmlService.cs),
invoked from `LoanProcessingXmlService` via `CreditBureauWorkerXml`. Both
services share the same attempt counter, stored in
`[dbo].[Katm_Methods_Request].ci017Attempt`, keyed by `loanKey`.

Business reported that CI-017 reports sit in the queue and don't get sent.

## Problem

Two independent defects cause this:

1. **Blocking poll loop.** `CreditReportStatus()` contains a `while` loop that,
   on a `WAIT_AND_TRY_AGAIN` result, does `Task.Delay(CheckReportStatusInterval)`
   (60s, appsettings.json) and retries in-process, up to `MaxCi017Attempts` (10)
   times. This method is awaited synchronously inside the `foreach` in
   `CreditBureauProcessing()` — so one application stuck waiting on the bureau
   blocks the entire queue for up to ~10 minutes. Multiple stuck applications
   compound this sequentially.

2. **Permanent stall after max attempts.** `GetLoanApplications()` selects
   applications purely by business `status` (00/01/02/03), which is unrelated
   to `ci017Attempt`. Once `ci017Attempt` reaches `MaxCi017Attempts`, the
   application keeps being re-selected by the worker forever, but
   `CreditReport()`/`CreditReportStatus()` short-circuit without contacting
   the bureau — even if whatever caused the failures has since been resolved
   and the business status changed. The only way out today is manual DB
   intervention.

## Goals

- No single application can block processing of other queued applications.
- Once an application's max CI-017 attempts are exhausted, it's marked as an
  error and left alone — the rest of the queue is unaffected (this already
  holds structurally once defect 1 is fixed, since `foreach` isolates
  per-item failures via try/catch).
- If an application's business status changes after exhausting attempts, CI-017
  processing automatically resumes for it — no manual reset required.
- The 60-second minimum interval between CI-017 status checks (bureau
  requirement, see inline comment in `CreditReport()`) is preserved even
  though we no longer block a worker iteration to wait it out.
- Reduce `MaxCi017Attempts` from 10 to 3 (agreed with business).
- Both the JSON pipeline (`CreditReportService`) and the XML pipeline
  (`CreditReportXmlService`) get the same fix, since they share the same
  `ci017Attempt` counter per `loanKey` — fixing only one would leave the
  other's behavior inconsistent and could clobber shared state.

## Non-Goals

- Not changing how `GetLoanApplications()` / `Loan_History_KB_Request` select
  which applications enter the queue.
- Not touching the unrelated CI-001..CI-023 report queue
  (`CreditBureauReportWorker` / `KATM_Report_*` table functions) — business
  confirmed the complaint is specific to CI-017.
- Not adding configurability for `MaxCi017Attempts` or `CheckReportStatusInterval`
  beyond what already exists — reusing the existing `CheckReportStatusInterval`
  setting and hard-coding the new attempt limit, matching the existing pattern.

## Design

### 1. Data model (`[dbo].[Katm_Methods_Request]`)

Add two nullable columns:

- `LastStatus NVARCHAR(10) NULL` — last business status seen for this
  `loanKey` when CI-017 processing last ran. Used to detect a status change
  and trigger an attempt-counter reset.
- `LastCi017AttemptAt DATETIME2 NULL` — timestamp of the last actual CI-017
  bureau call (report submit or status check) for this `loanKey`. Used to
  enforce the 60-second minimum interval between status checks now that the
  wait no longer happens inside a blocking loop.

### 2. Repository (`ICreditBureauReportRepository` / `CreditBureauReportRepository`)

- **New:** `Task<Ci017State> GetCi017StateAsync(int loanKey, CancellationToken ct)`
  returning `AttemptCount`, `LastStatus`, `LastAttemptAt` in a single query.
  Replaces all 4 existing call sites of `GetCi017AttemptCountAsync`
  (2 in `CreditReportService`, 2 in `CreditReportXmlService`).
  `GetCi017AttemptCountAsync` is removed since nothing else calls it.
- **New:** `Task ResetCi017AttemptAsync(int loanKey, string? newStatus, CancellationToken ct)`
  — single UPDATE: `ci017Attempt = 0`, `LastStatus = @newStatus`,
  `LastCi017AttemptAt = NULL`.
- **Changed:** `IncrementCi017AttemptAsync` additionally sets
  `LastCi017AttemptAt = SYSUTCDATETIME()` in the same UPDATE statement.

### 3. Service logic (identical change applied to `CreditReportService` and `CreditReportXmlService`)

**At the top of `CreditReport()` and `CreditReportStatus()`:**

1. Call `GetCi017StateAsync(loanKey, ct)`.
2. If `LastStatus` is `null` or `!= application.Status`:
   - `ResetCi017AttemptAsync(loanKey, application.Status, ct)`
   - `_notifiedMaxAttempts.TryRemove(loanKey, out _)` (so a future max-attempts
     breach notifies again — this field is on a Singleton-registered service,
     so without clearing it a second stall would notify nobody)
   - `UpsertCiStatusAsync(loanKey, 17, 0, "Status changed to {status}: attempts reset", null, ct)`
   - Treat `AttemptCount` as `0` and `LastAttemptAt` as `null` for the rest of
     this call.
3. `MaxCi017Attempts` constant: `10` → `3` in both services. Existing
   max-attempts branch (notify once via Telegram, `UpsertCiStatusAsync(loanKey, 17, 2, ...)`,
   return) is unchanged otherwise.

**`CreditReportStatus()` specific change:**

- Remove the `while (!cancellationToken.IsCancellationRequested)` loop
  entirely. The method makes exactly one attempt per call, matching the shape
  of `CreditReport()`.
- Before contacting the bureau: if `LastAttemptAt != null` and
  `DateTime.UtcNow - LastAttemptAt < TimeSpan.FromMilliseconds(_options.CheckReportStatusInterval)`,
  return immediately without calling the bureau, without incrementing the
  attempt count, and without logging (this is an expected, frequent no-op
  while waiting out the interval — not an error condition).
- On `WAIT_AND_TRY_AGAIN`: keep the existing `UpsertCiStatusAsync(loanKey, 17, 0, "Waiting", token, ct)`
  and `return` (no more `Task.Delay` + loop).
- `IncrementCi017AttemptAsync` (already called on every real attempt) now
  also stamps `LastCi017AttemptAt`, so the interval gate above works without
  extra plumbing.

### 4. Worker / config

- `MaxCi017Attempts` constant: `10` → `3` in both
  `Infrastructure\CreditReports\CreditReportService.cs` and
  `Infrastructure\CreditReportsXml\CreditReportXmlService.cs`.
- `WorkerSettings.DelayMilliseconds` (10s tick) and
  `CreditBureauReportApiOptions.CheckReportStatusInterval` (60s) are unchanged
  — the new `LastCi017AttemptAt` gate makes the 10s tick safe to run against
  the existing 60s bureau constraint without modifying either value.

### 5. Migration

New file `CreditBureauService\SqlMigrations\2026-07-10_add_ci017_retry_tracking.sql`,
following the existing style (see `2026-07-09_create_ci017_request_log.sql`):

```sql
ALTER TABLE [dbo].[Katm_Methods_Request]
ADD LastStatus NVARCHAR(10) NULL,
    LastCi017AttemptAt DATETIME2 NULL;
GO
```

## Testing

- Unit/integration coverage (or manual verification if the project lacks a
  test harness for this worker) for:
  - Status-change mid-stall resets the counter and resumes attempts.
  - Reaching 3 attempts marks the error status once, notifies once via
    Telegram once, and stops contacting the bureau.
  - `CreditReportStatus()` returns immediately (no bureau call) when called
    again inside the 60s window, and proceeds once the window has elapsed.
  - `CreditReportStatus()` no longer blocks the caller for more than one
    request/response round trip.
- Manual verification: run `CreditBureauWorker` locally against a mix of
  applications (one deliberately stuck) and confirm other queued applications
  are processed within one tick instead of waiting on the stuck one.

## Rollout Notes

- Run the migration before deploying the updated services (new columns are
  nullable, so this is backward compatible with the currently-running binary).
- No existing rows need backfilling — `LastStatus` starting as `NULL` for all
  existing rows is treated as "status changed," so every currently-stalled
  application gets one free reset on first pass after deploy. This is
  intentional and desired (unblocks the current backlog on deploy).
