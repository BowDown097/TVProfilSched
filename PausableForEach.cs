using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json.Linq;

namespace TVProfilSched;
class PausableForEach
{
    private readonly string channel, searchTerm;
    private readonly StreamWriter file;
    private bool pause;
    private readonly List<DateTime> processed = new();
    private readonly List<DateTime> source;

    public PausableForEach(List<DateTime> source, StreamWriter file, string channel, string searchTerm)
    {
        this.channel = channel;
        this.file = file;
        this.searchTerm = searchTerm;
        this.source = source;
        Run();
    }

    private static bool TryJObjectParse(string html, out JObject obj)
    {
        try
        {
            obj = JObject.Parse(html);
            return true;
        }
        catch (Exception)
        {
            obj = null;
            return false;
        }
    }

    private void Run()
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        ParallelLoopResult res = Parallel.ForEach(source.Except(processed.AsEnumerable()).AsParallel().AsOrdered(), (date, state) => {
            if (pause)
                return;

            ReqData data = new(date.ToString("yyyy-MM-dd"), channel);
            Console.WriteLine($"Parsing {data.datum}");
            var response = client.GetAsync($"https://tvprofil.com/tvprogram/program/?callback=tvprogramen{data.bCodeName}&datum={data.datum}&kanal={data.kanal}&{data.bCodeName}={data.bCode}").Result;
            string html = response.Content.ReadAsStringAsync().Result.Replace($"tvprogramen{data.bCodeName}(", "")[..^1];
            if (!TryJObjectParse(html, out JObject obj))
            {
                Console.WriteLine($"Problem occurred getting {data.datum}: Malformed data");
                return;
            }
            if (!response.IsSuccessStatusCode)
            {
                int code = obj["code"].Value<int>();
                if (code == 1226)
                {
                    pause = true;
                    state.Break();
                }
                else
                {
                    Console.WriteLine($"Problem occurred getting {data.datum}: Status code {response.StatusCode}");
                    return;
                }
            }

            string program;
            try { program = obj["data"]["program"].ToString(); } catch (Exception) { return; }
            IDocument document = BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(program)).Result;
            foreach (IElement row in document.QuerySelectorAll(".row"))
            {
                IElement col = row.QuerySelector(".col:not(.time)");
                IElement showElm = row.GetElementsByTagName("a").FirstOrDefault();
                string titles = col?.TextContent + (showElm?.GetAttribute("title") ?? "");
                if (titles.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(row.GetAttribute("data-ts")));
                    file.WriteLine($"{timestamp:s}: {col?.TextContent}");
                }
            }

            processed.Add(date);
        });

        if (source.Count > 0 && pause)
        {
            Console.WriteLine("Rate limited! Sleeping for 20 seconds...");
            Thread.Sleep(20000);
            pause = false;
            Run();
        }
    }
}