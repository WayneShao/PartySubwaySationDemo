using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaiduMapDemo
{
    public partial class PartySubwaySationForm : Form
    {

        public static string AK => "1lNWgYZosKTEnM794XmLG2zYGy6zSVNk";
        public static string SK => "gZG4OPlkB14CXn6sCbSRWZjCTzphC7FL";
        List<string> Data = new List<string>();

        string Randomstr = "功夫撒黑胡椒hcbvf蜂窝qwertyuiopasdfghjklzxcvbnm法国的恢复到飞范德萨QWERTYUIOPASDFGHJKLZXCVBNM出现过热423贴①46546也有一头热刚恢复到贴3天赋如头3广泛的我让他";


        Random rd = new Random(GetRandomSeed());

        static int GetRandomSeed()
        {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
        public PartySubwaySationForm()
        {
            InitializeComponent();
            textBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox1.AutoCompleteSource = AutoCompleteSource.CustomSource;
            textBox2.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox2.AutoCompleteSource = AutoCompleteSource.CustomSource;

            var stationsList = Lines.Select(l => l.Stations);
            var stations = new List<string>();
            foreach (var ss in stationsList)
                stations.AddRange(ss.Select(s => s.Name));

            StationNames = stations.Distinct().ToList();

            textBox1.AutoCompleteCustomSource.Clear();
            textBox1.AutoCompleteCustomSource.AddRange(StationNames.ToArray());
            textBox2.AutoCompleteCustomSource.Clear();
            textBox2.AutoCompleteCustomSource.AddRange(StationNames.ToArray());
        }

        public List<string> StationNames { get; set; }

        
        private void Form1_Load(object sender, EventArgs e)
        {
            //var r1 = PublicTest();
            //var r2 = RideTest();
            //var r3 = DriveTest();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public string json = "";
        private void button1_Click(object sender, EventArgs e)
        {
            //var res = ClientCreator.Create().Execute(new PlaceSuggestionRequest(new PlaceSuggestionModel()
            //{
            //    Query = textBox1.Text,
            //    Region = "广州"
            //}));

            //textBox1.AutoCompleteCustomSource.Clear();
            //textBox1.AutoCompleteCustomSource.AddRange(res.Result.Select(s => s.Name).ToArray());

            var jobject = JsonHelper.DeserializeObject(json);


            foreach (var l in jobject["subways"]["l"])
            {
                var line = new Line { Name = l["l_xmlattr"]["lb"].Value<string>() };
                foreach (var p in l["p"])
                {
                    var pname = p["p_xmlattr"]["lb"].Value<string>();
                    if (!string.IsNullOrWhiteSpace(pname) && p["p_xmlattr"]["uid"] != null)
                        line.Stations.Add(new Station
                        {
                            Name = pname,
                            Lines = p["p_xmlattr"]["lb"].Value<string>().Split(',').Select(s => s.Substring(s.IndexOf("|") + 1)).ToList(),
                            UId = p["p_xmlattr"]["uid"].Value<string>()
                        });
                }
                Lines.Add(line);
            }

            File.WriteAllText("Data/line.json", Lines.ToFormatedJsonString());

        }



        private List<Line> lines = null;
        public List<Line> Lines
        {
            get => lines ?? (lines = File.ReadAllText("Data/line.json").DeserializeFromJsonString<List<Line>>());
            set => File.WriteAllText("Data/line.json", (lines = value).ToJsonString());
        }

        public List<TwoStationInfo> twoStationInfos;
        public List<TwoStationInfo> TwoStationInfos
        {
            get => twoStationInfos ?? (twoStationInfos = File.ReadAllText("Data/info.json").DeserializeFromJsonString<List<TwoStationInfo>>());
            set => File.WriteAllText("Data/info.json", (twoStationInfos = value).ToJsonString());
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var stationsList = Lines.Select(l => l.Stations);
            var stations = new List<Station>();
            foreach (var ss in stationsList)
                stations.AddRange(ss);

            var client = new HttpClient();

            for (var i = 0; i < stations.Count; i++)
            {
                for (var j = 0; j < stations.Count; j++)
                {
                    if (i == j) continue;
                    if (TwoStationInfos.Any(ts => ts.TwoStationNames.Contains(stations[i].Name) && ts.TwoStationNames.Contains(stations[j].Name))) continue;
                    var result = await client.GetStringAsync(
                        $"https://api.map.baidu.com/?qt=bt2&newmap=1&ie=utf-8&f=%5B1,12,13,14%5D&c=257&sn=0$${stations[i].UId}$$undefined,undefined$${stations[i].Name}$$&en=0$${stations[j].UId}$$undefined,undefined$${stations[j].Name}$$&m=sbw&ccode=257&from=dtzt&sy=0&t=1552814046118&callback=BMapSub._rd._cbk22197");

                    var index = result.IndexOf('{');
                    result = result.Substring(index, result.Length - index - 1);

                    var jResult = JsonHelper.DeserializeObject(result);

                    if (jResult["content"] == null) continue;
                    var two = new TwoStationInfo
                    {
                        Time = jResult["content"][0]["exts"][0]["time"].Value<int>(),
                        Distance = jResult["content"][0]["exts"][0]["distance"].Value<int>(),
                        Price = jResult["content"][0]["exts"][0]["price"].Value<int>(),
                        SubwayPrice = jResult["content"][0]["exts"][0]["subway_price"].Value<int>(),
                        WalkDistance = jResult["content"][0]["exts"][0]["walk_distance"].Value<int>(),
                        WalkTime = jResult["content"][0]["exts"][0]["walk_time"].Value<int>(),
                        TwoStationNames = new[] { stations[i].Name, stations[j].Name }
                    };
                    TwoStationInfos.Add(two);

                    Console.WriteLine($@"{stations[i].Name} => {stations[j].Name}");

                }
            }
            File.WriteAllText("Data/info.json", TwoStationInfos.ToFormatedJsonString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (TwoStationInfos.Any(ts =>
                ts.TwoStationNames.Contains(textBox1.Text) && ts.TwoStationNames.Contains(textBox2.Text)))
            {
                var info = TwoStationInfos.First(ts =>
                    ts.TwoStationNames.Contains(textBox1.Text) && ts.TwoStationNames.Contains(textBox2.Text));

                textBox3.Text = info.ToFormatedJsonString();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var strs = listBox1.Items.Cast<string>().ToList();

            var routeInfos = StationNames.Select(n => GetRouteInfo(strs, n)).ToList();

            var minTime = routeInfos.Min(r => r.Routes.Sum(rr => rr.Time + rr.WalkTime));
            var minTimeRoute = routeInfos.First(r => r.Routes.Sum(rr => rr.Time + rr.WalkTime) == minTime);

            var minPrice = routeInfos.Min(r => r.Routes.Sum(rr => rr.Price));
            var minPriceRoute = routeInfos.First(r => r.Routes.Sum(rr => rr.Price) == minPrice);

            textBox3.Clear();

            textBox3.AppendText($"综合耗时最短：{minTimeRoute.DestStation} 耗时: {Second2Chs(minTime)} \r\n其中地铁时间: {Second2Chs(minTimeRoute.Routes.Sum(rr => rr.Time))} 步行时间:{Second2Chs(minTimeRoute.Routes.Sum(rr => rr.WalkTime))}\r\n\r\n");
            foreach (var info in minTimeRoute.Routes)
                textBox3.AppendText(
                    $"{info.TwoStationNames[1]}=>{info.TwoStationNames[0]} 耗时: {Second2Chs(info.Time + info.WalkTime)} \r\n其中地铁时间: {Second2Chs(info.Time)} 步行时间:{Second2Chs(info.WalkTime)} \r\n");
            textBox3.AppendText($"\r\n\r\n");
            textBox3.AppendText($"综合花费最少：{minPriceRoute.DestStation} 花费：{minPrice/100.0} \r\n\r\n");
            foreach (var info in minPriceRoute.Routes)
                textBox3.AppendText(
                    $"{info.TwoStationNames[1]}=>{info.TwoStationNames[0]} 花费: {info.Price / 100.0} \r\n");



        }

        private string Second2Chs(int second)
        {
            return new TimeSpan(0, 0, second).ToString("c");
        }
        private void button5_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
                listBox1.Items.Add(textBox1.Text);
        }

        private RouteInfo GetRouteInfo(List<string> deps, string dest)
        {
            var info = new RouteInfo
            {
                DestStation = dest,
                Routes = new List<TwoStationInfo>()
            };
            foreach (var dep in deps)
            {
                var two = GetRoute(dep, dest);
                if (two != null)
                    info.Routes.Add(two);
            }
            return info;
        }
        private TwoStationInfo GetRoute(string p1, string p2)
        {
            if (TwoStationInfos.Any(tsi => tsi.TwoStationNames.Contains(p1) && tsi.TwoStationNames.Contains(p2)))
                return TwoStationInfos.First(tsi => tsi.TwoStationNames.Contains(p1) && tsi.TwoStationNames.Contains(p2));

            return null;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
        }
    }

    public class RouteInfo
    {
        public string DestStation { get; set; }
        public List<TwoStationInfo> Routes { get; set; }
    }

    public class Line
    {
        public string Name { get; set; }
        public List<Station> Stations { get; set; } = new List<Station>();
    }

    public class Station
    {
        public string Name { get; set; }
        public string UId { get; set; }

        public List<string> Lines { get; set; }
    }

    public class TwoStationInfo
    {
        public string[] TwoStationNames { get; set; }
        public int Distance { get; set; }
        public int Price { get; set; }
        public int SubwayPrice { get; set; }
        public int Time { get; set; }
        public int WalkDistance { get; set; }
        public int WalkTime { get; set; }
    }
}
