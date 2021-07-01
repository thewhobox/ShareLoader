using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class StatisticShowModel
    {
        public int itemid { get; set; }
        public string id { get; set; } = "statisticChart" + new Random().Next(1000, 9999).ToString();
        public string width { get; set; } = "100%";
        public string height { get; set; } = "400px";

        public string type { get; set; } = "line";
        public StatisticData data { get; set; } = new StatisticData();

        public StatisticOptions options { get; set; } = new StatisticOptions();

        public string GetJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }

    public class StatisticOptions
    {
        public StatisticOptionsScales scales { get; set; } = new StatisticOptionsScales();
    }

    public class StatisticOptionsScales
    {
        public List<StatisticOptionsAxes> yAxes { get; set; } = new List<StatisticOptionsAxes>() { new StatisticOptionsAxes() };
    }

    public class StatisticOptionsAxes
    {
        public StatisticOptionsTicks ticks { get; set; } = new StatisticOptionsTicks();
    }

    public class StatisticOptionsTicks
    {
        public bool beginAtZero { get; set; } = true;
    }

    public class StatisticData
    {
        public List<string> labels { get; set; } = new List<string>();
        public List<StatisticDataset> datasets { get; set; } = new List<StatisticDataset>();
    }

    public class StatisticDataset
    {
        public string label { get; set; } = "undefined";
        public List<double> data { get; set; } = new List<double>();
        public string borderColor { get; set; }
        public string backgroundColor { get; set; }
        public int borderWidth { get; set; } = 1;
        public bool fill { get; set; } = false;

        public StatisticDataset()
        {
            int[] cols = GetRandomNumbers();
            borderColor = SetRandomColor(cols, "1");
            backgroundColor = SetRandomColor(cols, "0.5");
        }

        public int[] GetRandomNumbers()
        {
            Random rnd = new Random();
            return new int[] { rnd.Next(100, 255), rnd.Next(0, 255), rnd.Next(0, 255) };
        }

        public void SetColor(int r, int g, int b, int a = 1)
        {
            borderColor = "rgba(" + r + "," + g + "," + b + "," + a + ")";
        }

        private string SetRandomColor()
        {
            Random rnd = new Random();
            return "rgba(" + rnd.Next(0,255) + "," + rnd.Next(0, 255) + "," + rnd.Next(0, 255) + ", 1)";
        }

        private string SetRandomColor(int[] colors, string alpha = "1")
        {
            Random rnd = new Random();
            return "rgba(" + colors[0] + "," + colors[1] + "," + colors[2] + ", " + alpha + ")";
        }
    }
}