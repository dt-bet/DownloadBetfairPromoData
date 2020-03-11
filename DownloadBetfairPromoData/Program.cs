using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DownloadBetfairPromoData
{

    class Program
    {
        private const string betfair_url = "http://promo.betfair.com/betfairsp/prices";

        static void Main(string[] args)
        {

            System.IO.DirectoryInfo directory = System.IO.Directory.CreateDirectory("../../../Data");
            string file = Path.Combine(directory.FullName, "promobetfair.html");

            HttpClient client = new HttpClient();
            DownloadFile(file, client);

            var files = directory.GetFiles().Select(a => a.Name);
            var links = from l in GetLinks(System.IO.File.ReadAllText(file))
                        join a in files
                            on l.Split('/').Last() equals a into temp
                        where temp.Any() == false
                        select l;

            var enumtr = links.GetEnumerator();
            Observable.Interval(TimeSpan.FromSeconds(1))
                .TakeWhile((a) => enumtr.MoveNext())
                .SelectMany(a => GetContentAsync(client, enumtr.Current).ToObservable().Select(content => (link: enumtr.Current, content)))
                .Subscribe(a =>
                {
                    Console.WriteLine(a.Item1);
                    System.IO.File.WriteAllText(Path.Combine(directory.FullName, a.Item1.Split('/').Last()), a.content);
                }, e => Console.WriteLine(e.Message), () => Console.WriteLine("Completed"));

            Console.Read();

        }

        private static void DownloadFile(string file, HttpClient client)
        {
            if (File.Exists(file) == false)
            {
                var content = GetContentAsync(client, betfair_url);
                var awaiter = content.Result;
                File.WriteAllText(file, awaiter);
            }
        }

        private static IEnumerable<string> GetLinks(string s)
        {
            const string short_url = "https://promo.betfair.com";

            var linkParser = new Regex(@"\<a +href\=\""(.*)\""\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match m in linkParser.Matches(s))
            {
                yield return short_url + m.Groups[1].Value;
            }
        }

        private static async Task<string> GetContentAsync(HttpClient httpClient, string url)
        {
            using (var httpResponse = await httpClient.GetAsync(url).ConfigureAwait(false))
            {
                return await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
    }
}
