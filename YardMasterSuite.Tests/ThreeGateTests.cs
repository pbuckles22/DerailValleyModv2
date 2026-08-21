using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ThreeGateTests
{
    [Fact]
    public void TryApply_aborts_integrity_without_calling_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            integrityOk: false,
            stateRegistryOk: true,
            safetyOk: true,
            softWrite: () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Integrity, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void TryApply_applies_when_all_gates_pass()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(true, true, true, () =>
        {
            calls++;
            return true;
        });

        Assert.True(result.Applied);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void TryApply_aborts_when_soft_write_throws()
    {
        var result = ThreeGate.TryApply(true, true, true, () => throw new InvalidOperationException("boom"));
        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.SoftWrite, result.AbortReason);
    }
}
