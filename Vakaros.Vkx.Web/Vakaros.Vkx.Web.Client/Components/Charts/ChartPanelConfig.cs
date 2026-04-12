namespace Vakaros.Vkx.Web.Client.Components.Charts
{
    public class ChartSeriesConfig
    {
        public string Name { get; set; } = "";
        public string ValueField { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class ChartPanelConfig
    {
        public string YAxisLabel { get; set; } = "";
        public List<ChartSeriesConfig> Series { get; set; } = [];
        public List<ChartDataPoint> Data { get; set; } = [];
    }
}
