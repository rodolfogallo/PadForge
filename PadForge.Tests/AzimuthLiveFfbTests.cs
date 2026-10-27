using PadForge.Common.Input;
using PadForge.Engine;
using Xunit;

namespace PadForge.Tests;

public class AzimuthLiveFfbTests
{
    private static void Start(HMaestroFfbDecoder d, byte ebi, byte type = 1, short magnitude = 1000)
    {
        d.OnHidOutput(0x11, new byte[] { ebi, type, 255, 255 });
        d.OnHidOutput(0x15, new byte[] { ebi, (byte)magnitude, (byte)(magnitude >> 8) });
        d.OnHidOutput(0x1A, new byte[] { ebi, 1, 1 });
    }

    private static Vibration Read(HMaestroFfbDecoder d)
    {
        var v = new Vibration();
        d.Apply(v);
        return v;
    }

    [Fact]
    public void ZeroIndexConstantUpdateAfterStartReachesTheRunningEffect()
    {
        var decoder = new HMaestroFfbDecoder(null);
        decoder.OnHidOutput(0x1C, new byte[] { 4 });
        decoder.OnHidOutput(0x1D, new byte[] { 255 });
        decoder.OnHidOutput(0x15, new byte[] { 0, 0, 0 });
        var setup = new byte[21];
        setup[0] = 1; setup[1] = 1;
        setup[2] = 255; setup[3] = 255; setup[10] = 255;
        decoder.OnHidOutput(0x11, setup);
        decoder.OnHidOutput(0x1A, new byte[] { 1, 1, 1 });
        decoder.Apply(new Vibration());
        // Captured at 11:49:42: update only, no new Start follows.
        decoder.OnHidOutput(0x15, new byte[] { 0, 0x93, 0xFF });
        var output = new Vibration();
        decoder.ApplyIfDue(output);
        Assert.True(output.HasDirectionalData);
        Assert.Equal((short)-109, output.SignedMagnitude);
    }

    [Fact]
    public void LiveZeroUpdateSilencesAndSubsequentUpdateResumes()
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1);
        d.OnHidOutput(0x15, new byte[] { 0, 0, 0 });
        Assert.False(Read(d).HasDirectionalData);
        d.OnHidOutput(0x15, new byte[] { 0, 0xD0, 7 });
        Assert.Equal((short)2000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x1A, new byte[] { 1, 3 });
        Assert.False(Read(d).HasDirectionalData);
    }

    [Fact]
    public void MultipleEffectsKeepTheirMagnitudesUntilPendingWriteIsBound()
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1, magnitude: 1000);
        Start(d, 2, magnitude: 2000);
        d.OnHidOutput(0x15, new byte[] { 0, 0xB8, 0x0B }); // 3000
        Assert.Equal((short)2000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x1A, new byte[] { 1, 1, 1 });
        Assert.Equal((short)3000, Read(d).SignedMagnitude);
    }

    [Fact]
    public void NewAllocationBindsPendingWithoutChangingTheOldEffect()
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1, magnitude: 1000);
        d.OnHidOutput(0x15, new byte[] { 0, 0xB8, 0x0B });
        Assert.Equal((short)3000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x11, new byte[] { 2, 1, 255, 255 });
        Assert.Equal((short)1000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x1A, new byte[] { 2, 1, 1 });
        Assert.Equal((short)3000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x1A, new byte[] { 2, 3 });
        Assert.Equal((short)1000, Read(d).SignedMagnitude);
    }

    [Fact]
    public void PreallocationStillWaitsForCreationAndStart()
    {
        var d = new HMaestroFfbDecoder(null);
        d.OnHidOutput(0x15, new byte[] { 0, 0xD0, 7 });
        Assert.False(Read(d).HasDirectionalData);
        d.OnHidOutput(0x11, new byte[] { 1, 1, 255, 255 });
        Assert.False(Read(d).HasDirectionalData);
        d.OnHidOutput(0x1A, new byte[] { 1, 1, 1 });
        Assert.Equal((short)2000, Read(d).SignedMagnitude);
    }

    [Fact]
    public void ExplicitUpdateSupersedesTheLiveProjection()
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1);
        d.OnHidOutput(0x15, new byte[] { 0, 0xD0, 7 });
        d.OnHidOutput(0x15, new byte[] { 1, 0xB8, 0x0B });
        Assert.Equal((short)3000, Read(d).SignedMagnitude);
    }

    [Theory]
    [InlineData(2)] // Ramp
    [InlineData(4)] // Sine
    [InlineData(8)] // Spring
    public void ConstantUpdateDoesNotGuessAnotherEffectType(byte type)
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1, type);
        d.OnHidOutput(0x15, new byte[] { 0, 0xD0, 7 });
        Assert.Equal((short)1000, Read(d).SignedMagnitude);
    }

    [Fact]
    public void DeviceDisableAndResetStillSilenceLiveUpdates()
    {
        var d = new HMaestroFfbDecoder(null);
        Start(d, 1);
        d.OnHidOutput(0x15, new byte[] { 0, 0xD0, 7 });
        d.OnHidOutput(0x1C, new byte[] { 2 }); // disable actuators
        Assert.False(Read(d).HasDirectionalData);
        d.OnHidOutput(0x1C, new byte[] { 1 });
        Assert.Equal((short)2000, Read(d).SignedMagnitude);
        d.OnHidOutput(0x1C, new byte[] { 4 }); // reset
        Assert.False(Read(d).HasDirectionalData);
    }
}
