using System;
using System.Collections.Generic;
using System.ComponentModel;
using Flawright.Internals;
using Xunit;

namespace Flawright.UnitTests.Internals;

public class ProcessAttachRetryTests
{
    private static readonly int[] ExpectedTransientRetrySleeps = { 10, 20 };

    [Fact]
    public void Invoke_SuccessOnFirstAttempt_ReturnsValueWithoutSleeping()
    {
        var sleepCount = 0;
        var result = ProcessAttachRetry.Invoke(
            attach: () => 42,
            sleep: _ => sleepCount++);

        Assert.Equal(42, result);
        Assert.Equal(0, sleepCount);
    }

    [Fact]
    public void Invoke_TransientPartialCopyThenSuccess_RetriesAndReturns()
    {
        var attempts = 0;
        var sleeps = new List<int>();

        var result = ProcessAttachRetry.Invoke(
            attach: () =>
            {
                attempts++;
                if (attempts < 3)
                    throw new Win32Exception(299, "Only part of a ReadProcessMemory or WriteProcessMemory request was completed.");
                return "ok";
            },
            sleep: sleeps.Add);

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
        Assert.Equal(ExpectedTransientRetrySleeps, sleeps);
    }

    [Fact]
    public void Invoke_AllAttemptsExhausted_RethrowsLastWin32Exception()
    {
        var attempts = 0;

        var ex = Assert.Throws<Win32Exception>(() =>
            ProcessAttachRetry.Invoke<int>(
                attach: () =>
                {
                    attempts++;
                    throw new Win32Exception(299, "partial copy");
                },
                maxAttempts: 3,
                sleep: _ => { }));

        Assert.Equal(299, ex.NativeErrorCode);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public void Invoke_NonPartialCopyError_RethrowsImmediately()
    {
        var attempts = 0;

        var ex = Assert.Throws<Win32Exception>(() =>
            ProcessAttachRetry.Invoke<int>(
                attach: () =>
                {
                    attempts++;
                    throw new Win32Exception(5, "ACCESS_DENIED");
                },
                sleep: _ => { }));

        Assert.Equal(5, ex.NativeErrorCode);
        Assert.Equal(1, attempts);  // No retry on non-299 errors
    }

    [Fact]
    public void Invoke_NonWin32Exception_RethrowsImmediately()
    {
        var attempts = 0;

        Assert.Throws<InvalidOperationException>(() =>
            ProcessAttachRetry.Invoke<int>(
                attach: () =>
                {
                    attempts++;
                    throw new InvalidOperationException("not transient");
                },
                sleep: _ => { }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void Invoke_TransientPartialCopyTwice_FiresOnRetryTwice()
    {
        var attempts = 0;
        var retryEvents = new List<(int AttemptNumber, int DelayMs, int ErrorCode)>();

        var result = ProcessAttachRetry.Invoke(
            attach: () =>
            {
                attempts++;
                if (attempts < 3)
                    throw new Win32Exception(299, "Only part of a ReadProcessMemory or WriteProcessMemory request was completed.");
                return "ok";
            },
            sleep: _ => { },
            onRetry: (attemptNumber, delayMs, errorCode) =>
            {
                retryEvents.Add((attemptNumber, delayMs, errorCode));
            });

        Assert.Equal("ok", result);
        Assert.Equal(2, retryEvents.Count);
        Assert.Equal((1, 10, 299), retryEvents[0]);
        Assert.Equal((2, 20, 299), retryEvents[1]);
    }
}
