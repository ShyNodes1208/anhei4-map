using Anhei4Map.Core.Models;

namespace Anhei4Map.Tests;

public class AppSettingsTests
{
    [Fact]
    public void CreateDefaults_HasEightHotkeyBindings()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.Equal(8, settings.Hotkeys.Count);
    }

    [Fact]
    public void CreateDefaults_PlacementIs640x360()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.Equal(640, settings.Placement.Width);
        Assert.Equal(360, settings.Placement.Height);
    }

    [Fact]
    public void CreateDefaults_OpacityIs0_9()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.Equal(0.9, settings.Placement.Opacity);
    }

    [Fact]
    public void CreateDefaults_ZoomLevelIs1_0()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.Equal(1.0, settings.ZoomLevel);
    }

    [Fact]
    public void CreateDefaults_InitialStateIsLocked()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.Equal(WindowState.Locked, settings.InitialState);
    }

    [Fact]
    public void Validate_RejectsWidthBelow200()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with
        {
            Placement = defaults.Placement with { Width = 199 }
        };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_RejectsHeightBelow150()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with
        {
            Placement = defaults.Placement with { Height = 149 }
        };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_RejectsOpacityBelow0_1()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with
        {
            Placement = defaults.Placement with { Opacity = 0.09 }
        };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_RejectsOpacityAbove1_0()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with
        {
            Placement = defaults.Placement with { Opacity = 1.01 }
        };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_RejectsZoomBelow0_25()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with { ZoomLevel = 0.24 };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_RejectsZoomAbove5_0()
    {
        var defaults = AppSettings.CreateDefaults();
        var settings = defaults with { ZoomLevel = 5.01 };

        Assert.False(settings.Validate());
    }

    [Fact]
    public void Validate_AcceptsValidSettings()
    {
        var settings = AppSettings.CreateDefaults();

        Assert.True(settings.Validate());
    }
}
