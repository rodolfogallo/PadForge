using PadForge.Common.Input;
using PadForge.Engine;
using Xunit;

namespace PadForge.Tests;

public class DriveclubLiveFfbTests
{
    private static void Setup(HMaestroFfbDecoder d, byte ebi, byte type)
    {
        var data = new byte[21];
        data[0] = ebi; data[1] = type;
        data[2] = 255; data[3] = 255; data[10] = 255;
        data[14] = 0x20; // 8192 HID polar: force along the wheel's X axis
        d.OnHidOutput(0x11, data);
        d.OnHidOutput(0x1A, new byte[] { ebi, 1, 1 });
    }

    private static void Constant(HMaestroFfbDecoder d, short value, byte ebi = 0)
        => d.OnHidOutput(0x15, new byte[] { ebi, (byte)value, (byte)(value >> 8) });

    private static void Periodic(HMaestroFfbDecoder d, ushort value = 1124)
        => d.OnHidOutput(0x14, new byte[] { 0, (byte)value, (byte)(value >> 8), 0, 0, 0, 0, 82, 0, 0, 0 });

    private static HMaestroFfbDecoder FiveEffects()
    {
        var d = new HMaestroFfbDecoder(null);
        Constant(d, 0);
        Setup(d, 1, 1);
        Setup(d, 2, 8);
        Setup(d, 3, 9);
        Periodic(d, 0); Setup(d, 4, 4);
        Periodic(d, 0); Setup(d, 5, 4);
        return d;
    }

    private static Vibration Read(HMaestroFfbDecoder d)
    {
        var output = new Vibration();
        d.Apply(output);
        return output;
    }

    [Fact]
    public void ConstantUpdatesLiveWithSpringDamperAndTwoPeriodicEffects()
    {
        var d = FiveEffects();
        Constant(d, 4833); // captured 00 E1 12
        Assert.Equal((short)4833, Read(d).SignedMagnitude);
        Constant(d, -945); // captured 00 4F FC
        Assert.Equal((short)-945, Read(d).SignedMagnitude);
    }

    [Fact]
    public void PeriodicAndConditionUpdatesDoNotEraseLiveConstantOrItsZero()
    {
        var d = FiveEffects();
        Constant(d, 4833);
        Periodic(d);
        Assert.Equal((short)4833, Read(d).SignedMagnitude);
        d.OnHidOutput(0x13, new byte[] { 0, 15, 0, 0, 93, 1, 93, 1, 16, 39, 16, 39, 0, 0 });
        Assert.Equal((short)4833, Read(d).SignedMagnitude);
        Constant(d, 0);
        Periodic(d);
        Assert.False(Read(d).HasDirectionalData);
    }

    [Fact]
    public void PendingPeriodicCannotBecomeAConstantOnPauseReconfiguration()
    {
        var d = FiveEffects();
        Constant(d, -945);
        Periodic(d, 2000);
        // Recreating/restarting the constant must not bind a periodic magnitude.
        Setup(d, 1, 1);
        Assert.Equal((short)-945, Read(d).SignedMagnitude);
        Constant(d, 0);
        Setup(d, 1, 1);
        Assert.False(Read(d).HasDirectionalData);
    }

    [Fact]
    public void ConstantPendingIsNotConsumedByUnrelatedEffectCreation()
    {
        var d = new HMaestroFfbDecoder(null);
        Constant(d, -945);
        Setup(d, 2, 9);
        Assert.False(Read(d).HasDirectionalData);
        Setup(d, 1, 1);
        Assert.Equal((short)-945, Read(d).SignedMagnitude);
    }

    [Fact]
    public void ExplicitNewerConstantRemainsAppliedAfterOtherUpdates()
    {
        var d = FiveEffects();
        Constant(d, 4833);
        Constant(d, -945, 1);
        Periodic(d);
        Assert.Equal((short)-945, Read(d).SignedMagnitude);
        Setup(d, 1, 1);
        Assert.Equal((short)-945, Read(d).SignedMagnitude);
    }

    [Fact]
    public void SteeringAndDamperKeepIndependentTypesAndZeroSteeringDoesNotLatch()
    {
        var d = new HMaestroFfbDecoder(null);
        Setup(d, 1, 1);
        d.OnHidOutput(0x13, new byte[] { 0, 15, 0, 0, 93, 1, 93, 1, 16, 39, 16, 39, 0, 0 });
        Setup(d, 2, 9);
        Constant(d, -945);
        var v = Read(d);
        Assert.True(v.HasDirectionalData);
        Assert.True(v.HasConditionData);
        Assert.Equal(FfbEffectTypes.Const, v.EffectType);
        Assert.Equal(FfbEffectTypes.Damper, v.ConditionEffectType);
        Assert.Equal((short)-945, v.SignedMagnitude);
        Assert.Equal((short)349, v.ConditionAxes[0].PositiveCoefficient);
        Assert.NotEqual((short)0, ForceFeedbackState.ComputeWheelSteeringLevel(v, 100));
        Constant(d, 0);
        v = Read(d);
        Assert.False(v.HasDirectionalData);
        Assert.True(v.HasConditionData);
        Assert.Equal(FfbEffectTypes.Damper, v.ConditionEffectType);
        d.OnHidOutput(0x1C, new byte[] { 5 });
        v = Read(d);
        Assert.False(v.HasDirectionalData);
        Assert.False(v.HasConditionData);
    }

    [Theory]
    [InlineData(2)] // disable
    [InlineData(3)] // stop all
    [InlineData(4)] // reset
    [InlineData(5)] // pause
    public void DeviceSilencingStillWorksWithMixedEffects(byte op)
    {
        var d = FiveEffects();
        Constant(d, 4833);
        d.OnHidOutput(0x1C, new byte[] { op });
        Assert.False(Read(d).HasDirectionalData);
    }
}
