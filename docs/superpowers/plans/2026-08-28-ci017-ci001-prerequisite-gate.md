# CI-017 CI-001/CI-002 Prerequisite Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop CI-017 (credit report request) from being sent to the bureau before the loan's CI-001 (individual) or CI-002 (entity) registration has been confirmed, and immediately mark `Request_History.Status = "09"` when that happens.

**Architecture:** Add a single guard at the top of `CreditReportService.CreditReport()` that reads the existing (currently unused) `GetCreditBureau001StatusAsync` / `GetCreditBureau002StatusAsync` repository methods, picking which one to call based on `LoanApplication.ApplicationsSubjectType`. If the prerequisite isn't confirmed (`!= 1`), skip the bureau call and update `Request_History` status instead.

**Tech Stack:** C# / .NET, xUnit, Moq.

## Global Constraints

- `ApplicationsSubjectType == "0"` means физ.лицо (individual) → prerequisite is CI-001; any other value means юр.лицо (entity) → prerequisite is CI-002. (Spec: docs/superpowers/specs/2026-08-28-ci017-ci001-prerequisite-gate.md)
- Prerequisite confirmed means the repository status value equals `1` (byte). `null` or `0` both count as "not confirmed".
- `CreditReportStatus()` is out of scope for this change — do not modify it.

---

### Task 1: Gate CI-017 on CI-001/CI-002 confirmation

**Files:**
- Modify: `Infrastructure/CreditReports/CreditReportService.cs:40-53` (top of `CreditReport` method)
- Modify: `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`

**Interfaces:**
- Consumes (already exist on `ICreditBureauReportRepository`, `Application/Repositories/CreditBureauReportRepositories/ICreditBureauReportRepository.cs`):
  - `Task<byte?> GetCreditBureau001StatusAsync(int loanKey, CancellationToken cancellationToken)`
  - `Task<byte?> GetCreditBureau002StatusAsync(int loanKey, CancellationToken cancellationToken)`
  - `Task UpdateRequestHistoryStatusAsync(int loanKey, string status, CancellationToken cancellationToken)` (already used elsewhere in this file for the max-attempts case)
- Produces: no new public surface — internal behavior change only.

- [ ] **Step 1: Write the failing test for the individual (CI-001) case**

Add to `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`, after the last existing test in the class (before the closing `}` of the class):

```csharp
    [Fact]
    public async Task CreditReport_WhenCi001NotConfirmedForIndividual_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "50",
            PClaimId = "claim-50",
            Status = "02",
            ApplicationsSubjectType = "0"
        };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)null);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(50, "09", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau002StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReport_WhenCi002NotConfirmedForEntity_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "51",
            PClaimId = "claim-51",
            Status = "02",
            ApplicationsSubjectType = "1"
        };

        repository.Setup(r => r.GetCreditBureau002StatusAsync(51, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)0);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(51, "09", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `dotnet test Infrastructure.Tests --filter "FullyQualifiedName~CreditReportServiceTests&(FullyQualifiedName~Ci001NotConfirmed|FullyQualifiedName~Ci002NotConfirmed)"`

Expected: FAIL — `requestManager.Verify(... Times.Never)` fails because `CreditReport()` currently calls `SendPostRequest` unconditionally (no gate exists yet), and `UpdateRequestHistoryStatusAsync(50/51, "09", ...)` is never called.

- [ ] **Step 3: Implement the gate**

In `Infrastructure/CreditReports/CreditReportService.cs`, the `CreditReport` method currently starts:

```csharp
        public async Task CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            var loanKey = int.Parse(loanApplications.KeyCreditBureauKb);
            var ci017State = await _creditBureauReportRepository.GetCi017StateAsync(loanKey, cancellationToken);
```

Change it to:

```csharp
        public async Task CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
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

            var ci017State = await _creditBureauReportRepository.GetCi017StateAsync(loanKey, cancellationToken);
```

Leave the rest of the method (the max-attempts check and everything below it) unchanged.

- [ ] **Step 4: Run the new tests to verify they pass**

Run: `dotnet test Infrastructure.Tests --filter "FullyQualifiedName~CreditReportServiceTests&(FullyQualifiedName~Ci001NotConfirmed|FullyQualifiedName~Ci002NotConfirmed)"`

Expected: PASS (2 tests)

- [ ] **Step 5: Run the full test file to find now-broken tests**

Run: `dotnet test Infrastructure.Tests --filter "FullyQualifiedName~CreditReportServiceTests"`

Expected: FAIL — the 5 pre-existing tests that call `sut.CreditReport(...)` now hit the new gate, because their `LoanApplication` doesn't set `ApplicationsSubjectType` (defaults to `null`, so `isIndividual` is `false`), and `GetCreditBureau002StatusAsync` is never mocked on those tests (Moq returns `null` by default), so `CreditReport()` now returns early instead of reaching the bureau call it's asserting on.

- [ ] **Step 6: Fix the 5 pre-existing `CreditReport(...)` tests**

In `Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs`, add `ApplicationsSubjectType = "0"` to each of these 5 `LoanApplication` initializers, and a matching `GetCreditBureau001StatusAsync` mock returning `(byte?)1`, right before their existing `GetCi017StateAsync` setup:

1. `CreditReport_WhenAttemptsExhaustedUnderPreviousStatus_DoesNotResetCounterAndSkipsBureauCall` (loan key `"42"`):

```csharp
        var application = new LoanApplication { KeyCreditBureauKb = "42", PClaimId = "claim-42", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 5, LastStatus: "01", LastAttemptAt: null));
```

2. `CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau` (loan key `"43"`):

```csharp
        var application = new LoanApplication { KeyCreditBureauKb = "43", PClaimId = "claim-43", Status = "02", PToken = "tok-123", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(43, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        var ci017States = new Queue<Ci017State>(new[]
```

(keep the rest of that test body unchanged — this only adds the `ApplicationsSubjectType` field and the new mock line before the existing `ci017States` declaration)

3. `CreditReport_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndMarksError` (loan key `"7"`):

```csharp
        var application = new LoanApplication { KeyCreditBureauKb = "7", PClaimId = "claim-7", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));
```

4. `CreditReport_WhenAttemptsBelowNewLimitOfThree_StillCallsBureau` (loan key `"9"`):

```csharp
        var application = new LoanApplication { KeyCreditBureauKb = "9", PClaimId = "claim-9", Status = "02", PToken = "tok-123", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        var ci017States = new Queue<Ci017State>(new[]
```

5. `CreditReport_WhenAttemptsAtLimitCalledTwice_NotifiesTelegramOnlyOnce` (loan key `"11"`):

```csharp
        var application = new LoanApplication { KeyCreditBureauKb = "11", PClaimId = "claim-11", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));
```

- [ ] **Step 7: Run the full test file to verify everything passes**

Run: `dotnet test Infrastructure.Tests --filter "FullyQualifiedName~CreditReportServiceTests"`

Expected: PASS (all tests, including the 2 new ones and the 5 updated ones; `CreditReportStatus`-only tests are untouched and were never affected)

- [ ] **Step 8: Run the full solution test suite to check for unrelated regressions**

Run: `dotnet test`

Expected: PASS (no other test file references `CreditReportService.CreditReport` or the two prerequisite-status repository methods, per the earlier `Grep` — this step is a safety net, not expected to surface anything new)

- [ ] **Step 9: Commit**

```bash
git add Infrastructure/CreditReports/CreditReportService.cs Infrastructure.Tests/CreditReports/CreditReportServiceTests.cs
git commit -m "fix: block CI-017 until CI-001/CI-002 confirmed, mark Request_History 09"
```
