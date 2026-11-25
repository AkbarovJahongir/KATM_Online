---
title: CI-017 Immediate Status Check After Token Reception
date: 2026-08-10
status: Draft
---

## Overview

When CI-017 (credit bureau) returns status code 05050 ("The report awaits confirmation by the operator"), the system receives a token for later status checking. Currently, this token is saved but not used immediately. The requirement is to **immediately attempt to check the report status using the received token** before relying on periodic scheduler checks.

## Current Behavior

1. First request sent to CI-017 `/credit/report` endpoint
2. Response: `{"result": "05050", "token": "fxfvugf...", ...}`
3. Token saved to database via `KatmHelper(TypeOperation.Token)`
4. Status marked as "Waiting" with attempt count incremented
5. **Problem**: No further action until scheduler later calls `CreditReportStatus()` (uncertain timing)

## Desired Behavior

1. First request sent to CI-017 `/credit/report` endpoint
2. Response: `{"result": "05050", "token": "fxfvugf...", ...}`
3. Token saved to database
4. Status marked as "Waiting"
5. **NEW**: Immediately call `CreditReportStatus()` with the received token
6. Check if report is ready (05000 = Success)
7. If not ready yet, rely on existing scheduler/retry logic for remaining attempts (max 3 total)

## Implementation Details

### File: `Infrastructure/CreditReports/CreditReportService.cs`

In the `CreditReport()` method, after receiving 05050 response (around line 129-138):

**Before:**
```csharp
else if (baseResponse?.data?.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
{
    if (!string.IsNullOrEmpty(baseResponse.data.token))
    {
        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
        return;  // ← Currently returns here
    }
}
```

**After:**
```csharp
else if (baseResponse?.data?.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
{
    if (!string.IsNullOrEmpty(baseResponse.data.token))
    {
        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
        
        // NEW: Immediately check status with received token
        await CreditReportStatus(loanApplications, cancellationToken);
        return;
    }
}
```

### Retry Mechanism

- **Total attempts allowed**: 3 (controlled by `MaxCi017Attempts = 3`)
- **First attempt**: During initial `CreditReport()` call (after receiving token)
- **Second & third attempts**: Via existing scheduler logic (controlled by `CheckReportStatusInterval`)
- Each call to `CreditReportStatus()` increments the attempt counter via `IncrementCi017AttemptAsync()`

### Error Handling

All error cases already handled by existing `CreditReportStatus()` method:
- 05000 (Success): Save base64 report
- 05050 (Still waiting): Keep token, increment attempts
- Other statuses: Handle per existing logic

### Logging

No additional logging needed — existing logs in `CreditReportService.cs` already capture:
- Request/response for initial call
- Request/response for status check
- Full log file: `CreditReport017FullLogFile`

## Testing

### Existing Tests
Tests in `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs` should be updated/verified:
- Existing test: "When receives 05050, saves token and returns" 
- **New verification**: Assert that after 05050 response, `CreditReportStatus()` was called

### Test Scenarios

1. **Happy path**: 05050 → token received → immediate status check → 05000 (success)
2. **Retry path**: 05050 → token received → immediate status check → 05050 (still waiting) → scheduler retries
3. **Max attempts**: 05050 → 05050 → 05050 → exceed max attempts, mark as failed

## Files Modified

- `Infrastructure/CreditReports/CreditReportService.cs` - Add `await CreditReportStatus()` call
- `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs` - Update/add tests to verify immediate status check

## No Changes Required

- `CreditReportStatus()` method — already complete, no changes needed
- Database schema — no changes needed (token already stored)
- Configuration — no new settings needed
- Other services — no impact

## Success Criteria

✅ When CI-017 returns 05050 with token, `CreditReportStatus()` is called immediately in the same execution  
✅ If report is ready (05000), it's saved without waiting for scheduler  
✅ Retry limit remains 3 attempts total  
✅ Existing tests pass with updated expectations  
✅ New test verifies immediate status check behavior
