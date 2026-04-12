using Microsoft.JSInterop;
using System.Text.Json;
using Vakaros.Vkx.Shared;

namespace Vakaros.Vkx.Web.Client.Services;

public class UserPreferencesService(IJSRuntime js)
{
    private const string StorageKey = "user-preferences";
    private bool _loaded;

    public SpeedUnit SpeedUnit { get; private set; } = SpeedUnit.Knots;
    public WindSpeedUnit WindSpeedUnit { get; private set; } = WindSpeedUnit.Knots;
    public DistanceUnit DistanceUnit { get; private set; } = DistanceUnit.Meters;

    public async Task LoadAsync()
    {
        if (_loaded) return;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (json is not null)
            {
                var stored = JsonSerializer.Deserialize<StoredPreferences>(json);
                if (stored is not null)
                {
                    SpeedUnit = stored.SpeedUnit;
                    WindSpeedUnit = stored.WindSpeedUnit;
                    DistanceUnit = stored.DistanceUnit;
                }
            }
        }
        catch
        {
            // JS interop is unavailable during server prerendering or if localStorage is
            // blocked; fall back to defaults.
        }
        _loaded = true;
    }

    public async Task SetSpeedUnitAsync(SpeedUnit unit)
    {
        SpeedUnit = unit;
        await PersistAsync();
    }

    public async Task SetWindSpeedUnitAsync(WindSpeedUnit unit)
    {
        WindSpeedUnit = unit;
        await PersistAsync();
    }

    public async Task SetDistanceUnitAsync(DistanceUnit unit)
    {
        DistanceUnit = unit;
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(new StoredPreferences(SpeedUnit, WindSpeedUnit, DistanceUnit));
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    private record StoredPreferences(SpeedUnit SpeedUnit, WindSpeedUnit WindSpeedUnit, DistanceUnit DistanceUnit);
}
