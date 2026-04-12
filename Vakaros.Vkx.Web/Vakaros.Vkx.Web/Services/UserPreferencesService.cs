using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Vakaros.Vkx.Web.Services;

public class UserPreferencesService(ProtectedLocalStorage localStorage)
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
            var result = await localStorage.GetAsync<StoredPreferences>(StorageKey);
            if (result.Success && result.Value is not null)
            {
                SpeedUnit = result.Value.SpeedUnit;
                WindSpeedUnit = result.Value.WindSpeedUnit;
                DistanceUnit = result.Value.DistanceUnit;
            }
        }
        catch
        {
            // ProtectedLocalStorage can throw if the data-protection key changed; fall back to defaults.
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

    private Task PersistAsync()
        => localStorage.SetAsync(StorageKey, new StoredPreferences(SpeedUnit, WindSpeedUnit, DistanceUnit)).AsTask();

    private record StoredPreferences(SpeedUnit SpeedUnit, WindSpeedUnit WindSpeedUnit, DistanceUnit DistanceUnit);
}
