---
title: CI-017 Must Not Fire Before CI-001/CI-002 Confirmed
date: 2026-08-28
status: Draft
---

## Overview

`CreditReportService.CreditReport()` (CI-017, credit report request) currently
fires unconditionally whenever `LoanProcessingService.CreditBureauProcessing()`
sees a loan with `Status == "00"` — it calls `SenderClaimsAsync` (which only
flips `Request_History.Status` to `"02"`, it does not itself send anything)
and then immediately calls `CreditReport()` in the same iteration, regardless
of whether the actual CI-001/CI-002 registration request has been confirmed
by the bureau yet. This can send CI-017 for a loan whose registration hasn't
gone through.

## Desired Behavior

While the loan's prerequisite registration (CI-001 for individuals, CI-002
for legal entities) has not been confirmed (`ci_status != 1`), CI-017 must
not be sent to the bureau. Instead, `Request_History.Status` is immediately
updated to `"09"`.

## Implementation

### File: `Infrastructure/CreditReports/CreditReportService.cs`

At the top of `CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken)`,
before the existing max-attempts check:

```csharp
var loanKey = int.Parse(loanApplications.KeyCreditBureauKb);

var isIndividual = loanApplications.ApplicationsSubjectType == "0";
var prerequisiteStatus = isIndividual
    ? await _creditBureauReportRepository.GetCreditBureau001StatusAsync(loanKey, cancellationToken)
    : await _creditBureauReportRepository.GetCreditBureau002StatusAsync(loanKey, cancellationToken);

if (prerequisiteStatus != 1)
{
    await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(loanKey, "09", cancellationToken);
    return;
}
```

`ApplicationsSubjectType == "0"` means физ.лицо (individual) — this matches
the convention already documented elsewhere in `ICreditBureauReportRepository`
(CI-011/014/023: "для которых ci001 = 1 или ci002 = 1").

`GetCreditBureau001StatusAsync` / `GetCreditBureau002StatusAsync` already
exist on `ICreditBureauReportRepository` (querying `Katm_Methods_Request.ci001`
/ `.ci002`) but are currently unused anywhere in the codebase — this is their
first caller.

### No changes required

- `CreditReportStatus()` — already no-ops when `PToken` is null, which is the
  natural state for a loan whose CI-017 was never sent, so it can't leak a
  bureau call through that path either. It is not touched by this change.
- Database schema / migrations — no changes; the `ci001`/`ci002` columns and
  their accessors already exist.
- Other CI handlers — no impact.

## Testing

### File: `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`

- Update the 4 existing tests that call `sut.CreditReport(...)`
  (`CreditReport_WhenAttemptsExhaustedUnderPreviousStatus_...`,
  `CreditReport_WhenStatusChangedButAttemptsNotExhausted_...`,
  `CreditReport_WhenAttemptsAtNewLimitOfThree_...`,
  `CreditReport_WhenAttemptsBelowNewLimitOfThree_...`,
  `CreditReport_WhenAttemptsAtLimitCalledTwice_...`) to mock
  `GetCreditBureau001StatusAsync` (or `GetCreditBureau002StatusAsync`,
  depending on the `ApplicationsSubjectType` each test's `LoanApplication`
  ends up using) as returning `(byte?)1`, so the new gate doesn't change
  their existing behavior.
- Add a new test: when the prerequisite status is not `1` (null or `0`),
  `CreditReport()` must never call `SendPostRequest`, and must call
  `UpdateRequestHistoryStatusAsync(loanKey, "09", ...)` exactly once.
- Add a new test covering the legal-entity branch (`ApplicationsSubjectType
  == "1"`): prerequisite is read via `GetCreditBureau002StatusAsync`, not
  `GetCreditBureau001StatusAsync`.

## Success Criteria

✅ CI-017 is never sent to the bureau while the loan's CI-001 (individual) or
CI-002 (entity) hasn't been confirmed (`ci_status == 1`)
✅ In that case, `Request_History.Status` is set to `"09"` immediately, in the
same call
✅ Individual vs. entity loans are gated by the correct prerequisite
✅ Existing CI-017 behavior (max attempts, retry, immediate status check) is
unchanged once the prerequisite is satisfied
✅ All existing and new tests pass
