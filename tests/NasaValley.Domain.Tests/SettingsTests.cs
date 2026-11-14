using System.Collections.Generic;
using NasaValley.Domain;
using Xunit;

namespace NasaValley.Domain.Tests;

public class SettingsTests
{
    [Fact]
    public void RoundTripPreservesVolumeAndBindings()
    {
        var original = new AppSettingsData
        {
            MasterVolume = 0.42f,
            Bindings = new Dictionary<string, int> { ["move_up"] = 87, ["eat"] = 70 },
        };

        var json = SettingsSerializer.Serialize(original);
        var restored = SettingsSerializer.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(0.42f, restored!.MasterVolume);
        Assert.Equal(87, restored.Bindings["move_up"]);
        Assert.Equal(70, restored.Bindings["eat"]);
    }

    [Fact]
    public void CorruptOrIncompatibleFilesAreRejected()
    {
        Assert.Null(SettingsSerializer.Deserialize(""));
        Assert.Null(SettingsSerializer.Deserialize("not json"));
        Assert.NotNull(SettingsSerializer.Deserialize("{}"));
        Assert.Null(SettingsSerializer.Deserialize("""{"Version":999,"MasterVolume":0.5}"""));
    }

    [Fact]
    public void DefaultValuesAreSane()
    {
        var settings = new AppSettingsData();
        Assert.Equal(1, settings.Version);
        Assert.Equal(0.8f, settings.MasterVolume);
        Assert.Empty(settings.Bindings);
    }
}
