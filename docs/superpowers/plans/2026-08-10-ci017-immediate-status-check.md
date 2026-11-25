# CI-017 Immediate Status Check Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When CI-017 returns status 05050 with a token, immediately call CreditReportStatus() to check if the report is ready, instead of waiting for the scheduler.

**Architecture:** Add a synchronous call to `CreditReportStatus()` right after token is saved in the `CreditReport()` method when handling 05050 response. The existing `CreditReportStatus()` method already handles all status checks, retries, and error cases — no changes needed there.

**Tech Stack:** C#, .NET, xUnit/Moq for tests

## Global Constraints

- Total CI-017 attempts per loan: 3 (MaxCi017Attempts constant)
- First attempt happens in CreditReport() immediately after token reception
- Remaining attempts (2-3) handled by existing scheduler logic
- No database schema changes needed
- No new configuration needed

---

## Task 1: Add Immediate Status Check Call

**Files:**
- Modify: `Infrastructure/CreditReports/CreditReportService.cs:127-139`

**Interfaces:**
- Consumes: Existing `CreditReportStatus()` method (line 167+)
- Produces: Same CreditReport() behavior, plus additional CreditReportStatus() call when 05050 received

- [ ] **Step 1: Read current code around line 127-139**

Open `Infrastructure/CreditReports/CreditReportService.cs` and locate the section:
```csharp
else if (baseResponse?.data?.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
{
    if (!string.IsNullOrEmpty(baseResponse.data.token))
    {
        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
        return;  // ← Line 137
    }
}
```

- [ ] **Step 2: Add CreditReportStatus() call before return**

Replace the `return;` statement at line 137 with:
```csharp
else if (baseResponse?.data?.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
{
    if (!string.IsNullOrEmpty(baseResponse.data.token))
    {
        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
        
        // Immediately check report status with received token
        await CreditReportStatus(loanApplications, cancellationToken);
        return;
    }
}
```

This adds one line before the return:
```csharp
await CreditReportStatus(loanApplications, cancellationToken);
```

- [ ] **Step 3: Verify the change looks correct**

The modified section should now:
1. Call `KatmHelper()` to save token ✓
2. Call `UpsertCiStatusAsync()` to mark as "Waiting" ✓
3. **NEW**: Call `CreditReportStatus()` immediately ✓
4. Return without waiting for scheduler ✓

- [ ] **Step 4: Commit the code change**

```bash
git add Infrastructure/CreditReports/CreditReportService.cs
git commit -m "feat: immediately check CI-017 status after receiving token

When CI-017 returns 05050 (awaits operator confirmation) with a token,
immediately attempt to check the report status using that token instead
of waiting for the scheduler. This may reduce latency for reports that
are quickly confirmed by the operator.

- Add CreditReportStatus() call in CreditReport() after saving token
- Respects existing 3-attempt limit via IncrementCi017AttemptAsync()
- No changes to database schema or configuration
"
```

---

## Task 2: Update Test for 05050 Response

**Files:**
- Modify: `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`

**Interfaces:**
- Consumes: Updated `CreditReport()` method from Task 1
- Produces: Test that verifies `CreditReportStatus()` is called when 05050 received

- [ ] **Step 1: Locate the test around line 101-121**

Find the test named `CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau()` (around line 102-121). This test already sends a request and receives WaitAndTryAgainResponse.

- [ ] **Step 2: Add verification that CreditReportStatus is called**

At the end of the test (after line 120), add this verification:

```csharp
[Fact]
public async Task CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau()
{
    var (sut, requestManager, _, _, repository, _, _) = CreateSut();
    var application = new LoanApplication { KeyCreditBureauKb = "43", PClaimId = "claim-43", Status = "02" };

    repository.Setup(r => r.GetCi017StateAsync(43, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "01", LastAttemptAt: null));
    requestManager.Setup(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
        .ReturnsAsync(WaitAndTryAgainResponse);

    await sut.CreditReport(application, CancellationToken.None);

    repository.Verify(r => r.ResetCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    requestManager.Verify(r => r.SendPostRequest(
        It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
    repository.Verify(r => r.IncrementCi017AttemptAsync(43, "02", It.IsAny<CancellationToken>()), Times.Once);
    
    // NEW: Verify that after receiving 05050, CreditReportStatus is called immediately
    requestManager.Verify(r => r.SendPostRequest(
        "https://bureau.test/credit/report/status", It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
}
```

The new verification checks that `SendPostRequest` is called twice:
1. First call to `/credit/report` (the initial request)
2. Second call to `/credit/report/status` (the immediate status check)

- [ ] **Step 3: Mock the status check response**

For the immediate status check to work, the test needs to mock a response for the status check endpoint. Update the test setup to handle the second call:

```csharp
[Fact]
public async Task CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau()
{
    var (sut, requestManager, _, _, repository, _, _) = CreateSut();
    var application = new LoanApplication { KeyCreditBureauKb = "43", PClaimId = "claim-43", Status = "02", PToken = "tok-123" };

    repository.Setup(r => r.GetCi017StateAsync(43, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "01", LastAttemptAt: null));
    
    // First call returns 05050 with token
    requestManager.SetupSequence(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
        .ReturnsAsync(WaitAndTryAgainResponse)  // First call: /credit/report
        .ReturnsAsync(WaitAndTryAgainResponse); // Second call: /credit/report/status (still waiting)

    await sut.CreditReport(application, CancellationToken.None);

    repository.Verify(r => r.ResetCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    requestManager.Verify(r => r.SendPostRequest(
        It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Exactly(2));
    repository.Verify(r => r.IncrementCi017AttemptAsync(43, "02", It.IsAny<CancellationToken>()), Times.Exactly(2)); // Called twice now
}
```

Key changes:
- Add `PToken = "tok-123"` to application (needed for status check)
- Use `SetupSequence()` to return different responses for each call
- Verify `SendPostRequest` is called **twice** (was once)
- Verify `IncrementCi017AttemptAsync` is called **twice** (was once)

- [ ] **Step 4: Verify test compiles**

Run the test to ensure it compiles:
```bash
dotnet test Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs::CreditReportServiceTests::CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau -v
```

Expected: Test should pass or fail based on implementation, but should compile without errors.

- [ ] **Step 5: Commit the test changes**

```bash
git add Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs
git commit -m "test: verify immediate status check call when receiving 05050

When CreditReport() receives 05050 status with token, it should
immediately call CreditReportStatus() to check if report is ready.

- Update test to mock two calls: /credit/report and /credit/report/status
- Verify both SendPostRequest and IncrementCi017AttemptAsync called twice
- PToken provided in LoanApplication for status check
"
```

---

## Task 3: Run All CI-017 Tests

**Files:**
- Test: `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`

**Interfaces:**
- Consumes: Updated CreditReportService and test from Tasks 1-2
- Produces: All tests passing

- [ ] **Step 1: Run all CreditReportService tests**

```bash
dotnet test Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs -v
```

Expected output should show:
- ✓ CreditReport_WhenAttemptsExhaustedUnderPreviousStatus_DoesNotResetCounterAndSkipsBureauCall
- ✓ CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau
- ✓ CreditReport_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndMarksError
- ✓ CreditReport_WhenAttemptsBelowNewLimitOfThree_StillCallsBureau
- (and other existing tests)

All tests should pass. If any fail, check that:
1. `PToken` is set on `LoanApplication` when needed
2. `SetupSequence()` provides responses for both calls
3. Mocks are set up correctly for status check endpoint

- [ ] **Step 2: Run XML variant tests (if they exist)**

If there are `CreditReportXmlServiceTests.cs`, run those too:

```bash
dotnet test Infrastructure.Tests/CreditReportsXml/CreditReportXmlServiceTests.cs -v
```

Expected: All tests pass (XML variant should behave the same way)

- [ ] **Step 3: Note any test failures and debug**

If a test fails:
1. Check the error message
2. Verify the mock setup matches the new call pattern (two calls instead of one)
3. Ensure `CreditReportStatus()` can find the token in `loanApplications.PToken`
4. Confirm `IncrementCi017AttemptAsync()` is called the right number of times

- [ ] **Step 4: Commit passing tests**

Once all tests pass:

```bash
git add -A
git commit -m "test: all CI-017 tests passing with immediate status check

All existing tests updated and passing:
- CreditReport with 05050 response now calls CreditReportStatus immediately
- Attempt counter correctly incremented twice (once per request)
- Retry logic and max-attempts enforcement working as expected
"
```

---

## Task 4: Verify No Regressions in Related Tests

**Files:**
- Test: `Infrastructure.Tests/**/*CreditReport*.cs`

**Interfaces:**
- Consumes: All changes from Tasks 1-3
- Produces: All credit report tests passing, no side effects

- [ ] **Step 1: Run all credit report related tests**

```bash
dotnet test Infrastructure.Tests/ -k "CreditReport" -v
```

This runs all test files matching "CreditReport" pattern. Expected: All tests pass.

- [ ] **Step 2: Check for any unexpected behavior**

Look for:
- Tests that weren't modified but now fail → unexpected side effect
- Tests for other CI handlers (CI-001, CI-002, etc.) → should not be affected
- Tests for status checking logic → should work the same

If you see failures:
1. Review the test name to understand what it's testing
2. Check if our changes affected that code path
3. Update mocks or test expectations if needed
4. Add verification for the immediate status check if it's a relevant test

- [ ] **Step 3: Commit if no regressions**

```bash
git add -A
git commit -m "test: verify no regressions in credit report tests

Ran full credit report test suite - all tests pass.
Changes to CreditReport() don't affect other handlers or status checking paths.
"
```

---

## Task 5: Final Integration Smoke Test (Manual)

**Files:**
- Code: `Infrastructure/CreditReports/CreditReportService.cs`
- Tests: `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`

**Interfaces:**
- Consumes: All changes from Tasks 1-4
- Produces: Verification that the feature works end-to-end

- [ ] **Step 1: Review the code flow one more time**

Open `Infrastructure/CreditReports/CreditReportService.cs` and trace through:

1. **CreditReport() method (line 40)**:
   - Sends first request to `/credit/report`
   - Receives 05050 with token
   - Saves token via `KatmHelper()`
   - Marks as "Waiting" via `UpsertCiStatusAsync()`
   - **Calls** `CreditReportStatus()` ← Our change
   - Returns

2. **CreditReportStatus() method (line 167)**:
   - Checks if max attempts reached → returns if yes
   - Checks time interval → returns if too soon
   - Sends request to `/credit/report/status` with token
   - Handles responses (05000=success, 05050=still waiting, etc.)
   - Increments attempt counter
   - Returns

Flow should look correct with immediate status check integrated.

- [ ] **Step 2: Verify variable scope and types**

Check:
- `loanApplications` object passed to `CreditReportStatus()` contains all needed data ✓
- `loanApplications.PToken` will be populated when loading the loan (from earlier code) ✓
- `CreditBureauResultCodes.WAIT_AND_TRY_AGAIN` value is "05050" ✓
- `MaxCi017Attempts = 3` constant is enforced in both methods ✓

- [ ] **Step 3: Check for any await/async issues**

Verify:
- `await CreditReportStatus()` is properly awaited ✓
- `CreditReportStatus()` is an async method ✓
- CancellationToken is threaded through ✓
- No fire-and-forget tasks (no `Task.Run()` without await) ✓

- [ ] **Step 4: Create final summary commit**

```bash
git log --oneline -5
```

Should show:
1. Task 1: feat: immediately check CI-017 status after receiving token
2. Task 2: test: verify immediate status check call when receiving 05050
3. Task 3: test: all CI-017 tests passing with immediate status check
4. Task 4: test: verify no regressions in credit report tests

- [ ] **Step 5: Summary**

Implementation complete:
- ✅ Added immediate `CreditReportStatus()` call after token reception
- ✅ Tests updated to verify the behavior
- ✅ All tests passing
- ✅ No regressions in related code
- ✅ Code review ready

