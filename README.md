
# 聚会合租选点：计算距离多个地铁站点综合时间最少的聚会地点

## 缘起
　　五人小组的聚会由于最初不知为何定在了体育中心、体育西一带，之后一直延续这个传统，鉴于前几次的聚会高度重复，[聚会体验越来越差](https://blog.chcaty.cn/2019/02/24/%E8%81%9A%E4%BC%9A%E9%9A%8F%E6%83%B3/#%E5%85%B3%E4%BA%8E%E8%81%9A%E4%BC%9A)，便起了一个想法：
> 为什么不做一个小工具来计算相对几位参与人居住地点最划算的聚会地点呢？

<!--more-->
## 分析
　　在各大地图APP上都有提供各种行程规划的功能，但是利用这个功能来完成预期的业务希望是相当繁琐的，而这个功能如果要自己实现，需要得到的数据就是地铁路线图上任意两个点之间所需要的时间、购票费用等数据。
　　经过在网上的搜寻，我最终决定的数据来源方案为百度开放平台中的[地铁图JS API](http://lbsyun.baidu.com/jsdemo.htm#subway4_1)

## 抓取关键数据
    在源代码编辑器中将调试的城市名称改为＇广州＇，点击编辑器右上角的运行按钮，在调试窗口中就能找到相关的API请求，所有地铁站数据都可以在这次请求中获得。
　　![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317105515992.png)

实体类:
```csharp
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
```
将Json数据解析转换为实体类型并保存到文件中
```csharp
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
```

![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317110105885.png)

那么接下来我们就要获取两点之前的数据的方法：
![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317110533303.png)

同样，我们找到了页面请求这个数据的API
![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317110619477.png)
![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317110804123.png)
经测试，只要将圈中的数据替换为响应站点的UID和Name，即可获取到相应的数据。

实体类:
```csharp
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
```

抓取:
```csharp
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
```
最终得到了所有数据，$C_{224}^2$ 大约25000条：
![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317111150999.png)


## 计算
得到了数据，那计算就只是体力活了，计算代码如下：
```csharp
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
```

运行结果截图:
![](http://qiniucdn.wayneshao.com/聚会合租选点：计算距离多个地点综合时间最少的聚会地点/20190317112034374.png)


[代码：包含已抓取到的数据](https://github.com/WayneShao/PartySubwaySationDemo)

## 结论

1. 首先，软件还比较粗糙，只是简单地数据计算筛选，结果只能做参考。
2. 同样也可以用于在不同地点工作的人寻找合租的地点，但在这个应用中缺少权值计算，而且往往时间最短的地点都坐落于两条或者多条线的交界处，通勤时的等待时间往往也比较长。
3. 以后考虑为每个地点增加一些美食指数之类的多维度权值，可以按照不同需求求出最优解。
