using System.Text.Json.Serialization;

namespace Vakaros.Vkx.Web.Client.Components.Charts
{
    /// <summary>
    /// Generic chart data point. Set <see cref="Time"/> to an ISO-8601 string
    /// and populate <see cref="Values"/> with named numeric values whose keys
    /// match the <c>ValueField</c> of the corresponding <see cref="ChartSeriesConfig"/>.
    /// </summary>
    public class ChartDataPoint
    {
        public string Time { get; set; } = "";

        [JsonExtensionData]
        public Dictionary<string, object?> Values { get; set; } = [];
    }
}
