using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace PodDl
{
    class Program
    {
        private static string path = @"";
        private static string input = @"rss.txt";

        private static int connections = 5;

        private static int printcounter = 0;

        private static List<Download> todo = new List<Download>();
        private static List<Download> ongoing = new List<Download>();

        static void Main(string[] args)
        {
            PodDlParamsObject parobj = new PodDlParamsObject(args);
            try
            {
                parobj.CheckParams();

                string _helptext = parobj.GetHelpIfNeeded();

                //Print help to console if requested
                if (!string.IsNullOrEmpty(_helptext))
                {
                    Console.WriteLine(_helptext);
                    Environment.Exit(0);
                }

                path = parobj.OutputPath ?? path;
                connections = parobj.Connections == 0 ?  connections : parobj.Connections;
                input = parobj.InputPath ?? input;

                if (path != "" && path[path.Length - 1] != '\\')
                    path += @"\";


            }
            catch(Exception ex)
            {
                Log(ex.Message);
                Environment.Exit(0);
            }
            

            ServicePointManager.DefaultConnectionLimit = connections;

            //input = "podcasts.opml";

            if(input.ToLower().Contains(".opml"))
            {

                using (XmlReader reader = XmlReader.Create(input))
                {
                    while (reader.Read())
                    {
                        if(reader.Name == "outline")
                        {
                            if(reader.GetAttribute("type") == "rss" && !string.IsNullOrEmpty(reader.GetAttribute("xmlUrl")))
                            {
                                ProcessRSS(reader.GetAttribute("xmlUrl"));
                            }
                        }
                    }
                }
            }
            else
            {
                string line;
                StreamReader file = new StreamReader(input);
                while ((line = file.ReadLine()) != null)
                {
                    ProcessRSS(line);
                }

                file.Close();
            }

            Log($"Number of connections: {connections}");

            for (var i = 0; i < connections; i++)
                Download();

            Console.ReadLine();
        }

        private static void ProcessRSS(string url)
        {
            Log($"Processing url: {url}");

            var r = XmlReader.Create(url);
            var res = SyndicationFeed.Load(r);

            var podcast_title = res.Title.Text;

            Log($"Podcast {podcast_title} found containing {res.Items.Count()} episodes.");

            foreach (var item in res.Items)
            {
                var date = item.PublishDate.ToString("yyyy.MM.dd");
                var title = MakeValidFileName(item.Title.Text).Trim();

                var link = item.Links.Where(l => l.RelationshipType == "enclosure").FirstOrDefault();

                var newpath = path + MakeValidFileName(podcast_title);

                if (!Directory.Exists($"{newpath}"))
                {
                    Log($"Directory does not exist, creating.");
                    Directory.CreateDirectory(newpath);
                }

                if (link != null)
                {
                    var filename = $@"{newpath}\{date}-{title}.mp3";

                    if (!File.Exists(filename))
                    {
                        var dl = new Download
                        {
                            Id = item.Id,
                            Podcast = podcast_title,
                            Title = item.Title.Text,
                            Url = link.Uri,
                            Bytescompleted = 0L,
                            Percentcomplete = 0,
                            Bytestotal = 0L,
                            Filename = filename
                        };

                        Log($"Adding {podcast_title}: {item.Title.Text} to the download list");
                        todo.Add(dl);
                    }
                    else
                    {
                        Log($"{date}-{title}.mp3 already existst, skipping.");
                    }
                }
            }

            r.Close();
        }

        private static void Download()
        {
            if (ongoing.Count < connections)
            {
                using (var client = new WebClient())
                {
                    if(todo.Count > 0)
                    {
                        var dl = todo[0];
                        todo.RemoveAt(0);

                        client.DownloadProgressChanged += (o, e) =>
                        {
                            dl.Bytescompleted = e.BytesReceived;
                            dl.Bytestotal = e.TotalBytesToReceive;
                            dl.Percentcomplete = e.ProgressPercentage;

                            ShowDlProgress();
                        };

                        client.DownloadFileCompleted += (o, e) =>
                        {
                            lock (ongoing)
                            {
                                ongoing.Remove(dl);
                            }
                            Download();
                        };

                        Log($"Starting download of {dl.Podcast}: {dl.Title}");
                        lock (ongoing)
                        {
                            ongoing.Add(dl);
                        }

                        try
                        {
                            client.DownloadFileAsync(dl.Url, dl.Filename);
                        }
                        catch(Exception e)
                        {
                            Log($"Error downloading from {dl.Url.AbsoluteUri}: {e.Message}");
                        }
                    }
                    
                }
            }
        }

        private static void ShowDlProgress()
        {
            printcounter++;
            lock (ongoing)
            {
                if (printcounter % 100 == 0)
                {
                    Console.Clear();
                    Log($"Downloading to {path}", false);
                    foreach (var dl in ongoing)
                    {
                        Log($"Downloading {dl.Podcast}: {dl.Title} -> {((dl.Bytescompleted / 1024f) / 1024f).ToString("#0.##")}mb of {((dl.Bytestotal / 1024f) / 1024f).ToString("#0.##")}mb ({dl.Percentcomplete}%)", false);
                    }

                    if(ongoing.Count == 0)
                    {
                        Log("Downloads complete.");
                    }
                }
            }
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = "'" + new string(Path.GetInvalidFileNameChars());
            string escapedInvalidChars = Regex.Escape(invalidChars);
            string invalidRegex = string.Format(@"([{0}]*\.+$)|([{0}]+)", escapedInvalidChars);

            return Regex.Replace(name, invalidRegex, " ");
        }

        private static void Log(string logline, bool tofile = true)
        {
            Console.WriteLine(logline);
        }
    }
}
