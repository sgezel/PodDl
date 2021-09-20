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
using System.Xml.Linq;
using System.Xml.XPath;

namespace PodDl
{
    class Program
    {
        private static string path = @"";
        private static string input = @"rss.txt";
        private static bool generatedocumentation = true;
        private static bool skipDownload = false;
        private static string downloadArchive = @"";
        private static PodDlDownloadArchive archive = null;

        private static int connections = 5;

        private static int printcounter = 0;

        private static List<Download> todo = new List<Download>();
        private static List<Download> ongoing = new List<Download>();

        private static Documentation doc = new Documentation();

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
                connections = parobj.Connections == 0 ? connections : parobj.Connections;
                input = parobj.InputPath ?? input;
                generatedocumentation = parobj.Documentation;
                skipDownload = parobj.SkipFileDownload;
                downloadArchive = parobj.DownloadArchive;
                if (!String.IsNullOrEmpty(downloadArchive))
                {
                    archive = new PodDlDownloadArchive(downloadArchive);
                }

                if (path != "" && path[path.Length - 1] != '\\')
                    path += @"\";
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Environment.Exit(0);
            }

            ServicePointManager.DefaultConnectionLimit = connections;
            doc.Podcasts = new List<Podcast>();

            if (input.ToLower().Contains(".opml"))
            {
                using (XmlReader reader = XmlReader.Create(input))
                {
                    while (reader.Read())
                    {
                        if (reader.Name == "outline")
                        {
                            if (reader.GetAttribute("type") == "rss" && !string.IsNullOrEmpty(reader.GetAttribute("xmlUrl")))
                            {
                                String xmlUrl = reader.GetAttribute("xmlUrl");
                                String podcastTitle = reader.GetAttribute("text");

                                try
                                {
                                    ProcessRSS(xmlUrl, podcastTitle);
                                }
                                catch (Exception e)
                                {
                                    Log($"Failed reading {xmlUrl} for {podcastTitle}\n{e}");
                                }
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


            if (skipDownload)
            {
                Log($"Skipping all downloads");
                return;
            }

            Log($"Number of connections: {connections}");

            for (var i = 0; i < connections; i++)
                Download();

            if (generatedocumentation)
                GenerateDocumentation();

            Console.ReadLine();
        }


        private static void GenerateDocumentation()
        {
            doc.Podcasts = doc.Podcasts.OrderBy(p => p.Title).ToList();

            var index = doc.GenerateIndex();
            File.WriteAllText($"{path}index.html", index);

            if (!Directory.Exists($"{path}_documentation"))
                Directory.CreateDirectory($"{path}_documentation");

            foreach (var podcast in doc.Podcasts)
            {
                var details = doc.GeneratePodcastDetails(podcast);
                File.WriteAllText($"{path}_documentation/{podcast.DetailFileName}.html", details);
            }
        }

        private static void ProcessRSS(String url, String podcastTitle = "")
        {
            Log($"Processing url: {url}");

            // Use a custom PodDlXmlReader to avoid RSS XML issues
            SyndicationFeed res = null;
            try
            {
                using (var r = new PodDlXmlReader(url))
                {
                    res = SyndicationFeed.Load(r);
                }
            }
            catch (Exception)
            {
                string xml;

                using (WebClient webClient = new WebClient())
                {
                    xml = Encoding.UTF8.GetString(webClient.DownloadData(url));
                }

                xml = xml.Replace("length=\"None\"", "length=\"0\"");
                byte[] bytes = System.Text.UTF8Encoding.ASCII.GetBytes(xml);

                using (XmlReader r = XmlReader.Create(new MemoryStream(bytes)))
                {
                    res = SyndicationFeed.Load(r);
                }
            }

            podcastTitle = !String.IsNullOrEmpty(podcastTitle) ? podcastTitle : res.Title.Text;

            string extensionNamespaceUri = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            SyndicationElementExtension extension = res.ElementExtensions.Where<SyndicationElementExtension>(x => x.OuterNamespace == extensionNamespaceUri).FirstOrDefault();
            XPathNavigator dataNavigator = new XPathDocument(extension.GetReader()).CreateNavigator();

            XmlNamespaceManager resolver = new XmlNamespaceManager(dataNavigator.NameTable);
            resolver.AddNamespace("itunes", extensionNamespaceUri);

            XPathNavigator authorNavigator = dataNavigator.SelectSingleNode("itunes:author", resolver);

            var author = authorNavigator != null ? authorNavigator.Value : String.Empty;

            Log($"Podcast {podcastTitle} found containing {res.Items.Count()} episodes.");

            var podcast = new Podcast()
            {
                Title = podcastTitle,
                Author = author,
                Description = res.Description.Text,
                Image = res.ImageUrl != null ? res.ImageUrl.AbsoluteUri : $"https://via.placeholder.com/500x300/cccccc/000000?text={podcastTitle.Replace(" ", "+")}",
                Episodes = new List<Episode>(),
                Url = "#",
                DetailFileName = MakeValidFileName(podcastTitle)
            };

            foreach (var item in res.Items)
            {
                SyndicationElementExtension itemExtension = item.ElementExtensions.Where<SyndicationElementExtension>(x => x.OuterNamespace == extensionNamespaceUri).FirstOrDefault();
                XPathNavigator seasonNavigator = null;
                XPathNavigator episodeNavigator = null;
                XPathNavigator summaryNavigator = null;
                XPathNavigator imageNavigator = null;
                XPathNavigator durationNavigator = null;

                if (itemExtension != null)
                {
                    XPathNavigator itemDataNavigator = new XPathDocument(itemExtension.GetReader()).CreateNavigator();

                    XmlNamespaceManager itemResolver = new XmlNamespaceManager(itemDataNavigator.NameTable);
                    itemResolver.AddNamespace("itunes", extensionNamespaceUri);

                    seasonNavigator = itemDataNavigator.SelectSingleNode("itunes:season", itemResolver);
                    episodeNavigator = itemDataNavigator.SelectSingleNode("itunes:episode", itemResolver);
                    summaryNavigator = itemDataNavigator.SelectSingleNode("itunes:summary", itemResolver);
                    imageNavigator = itemDataNavigator.SelectSingleNode("itunes:image", itemResolver);
                    durationNavigator = itemDataNavigator.SelectSingleNode("itunes:duration", itemResolver);
                }

                var season = seasonNavigator != null ? seasonNavigator.Value : String.Empty;
                var episode = episodeNavigator != null ? episodeNavigator.Value : String.Empty;
                var summary = summaryNavigator != null ? summaryNavigator.Value : String.Empty;
                var image = imageNavigator != null ? imageNavigator.Value : podcast.Image;
                var duration = durationNavigator != null ? durationNavigator.Value : String.Empty;
                var guid = item.Id;

                var date = item.PublishDate.ToString("yyyy.MM.dd");
                var title = MakeValidFileName(item.Title.Text).Trim();

                var link = item.Links.Where(l => l.RelationshipType == "enclosure").FirstOrDefault();

                var newpath = path + MakeValidFileName(podcastTitle);

                if (!Directory.Exists($"{newpath}"))
                {
                    Log($"Directory does not exist, creating.");
                    Directory.CreateDirectory(newpath);
                }

                var season_string = "";

                if (!string.IsNullOrEmpty(season) && !string.IsNullOrEmpty(episode))
                    season_string = $"-S{season}E{episode}";

                if (link != null)
                {
                    var filename = $@"{newpath}\{date}-{title}.mp3";

                    podcast.Episodes.Add(new Episode()
                    {
                        Description = summary,
                        Title = item.Title.Text,
                        Duration = duration,
                        Image = (string.IsNullOrEmpty(image) ? res.ImageUrl == null ? image : res.ImageUrl.AbsoluteUri : image),
                        Url = $"..\\{ MakeValidFileName(podcastTitle)}\\{date}-{title}.mp3"
                    });

                    if (archive != null && archive.HasId(podcastTitle, item.Id))
                    {
                        Log($"{date}-{title}.mp3 already exists (by GUID), skipping.");
                    }
                    else if (!File.Exists(filename))
                    {
                        var dl = new Download
                        {
                            Id = item.Id,
                            Podcast = podcastTitle,
                            Title = item.Title.Text,
                            Url = link.Uri,
                            Bytescompleted = 0L,
                            Percentcomplete = 0,
                            Bytestotal = 0L,
                            Filename = filename,
                            Episode = episode,
                            Season = season,
                            Author = author,
                            Description = summary,
                            Image = image,
                            Year = item.PublishDate.Year
                        };

                        Log($"Adding {podcastTitle}: {item.Title.Text} to the download list");
                        todo.Add(dl);
                    }
                    else
                    {
                        Log($"{date}-{title}.mp3 already exists, skipping.");

                        // found it on the file system
                        archive.Add(podcastTitle, guid);
                        archive.Save();
                    }
                }
            }
            doc.Podcasts.Add(podcast);
        }

        private static void Download()
        {
            if (ongoing.Count < connections)
            {
                using (var client = new WebClient())
                {
                    if (todo.Count > 0)
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
                                try
                                {
                                    SetID3Tags(dl);
                                }
                                catch (Exception ex)
                                {
                                    Log($"Error setting id3 tags for {dl.Filename}:{Environment.NewLine}{ex.Message}");
                                }

                                ongoing.Remove(dl);

                                // Save to Download Archive
                                if (archive != null)
                                {
                                    archive.Add(dl.Podcast, dl.Id);
                                    archive.Save();
                                }
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
                        catch (Exception e)
                        {
                            Log($"Error downloading from {dl.Url.AbsoluteUri}: {e.Message}");
                        }
                    }
                }
            }

            if (ongoing.Count == 0)
            {
                Log("Downloads have been completed");

                Environment.Exit(0);
            }
        }

        private static void SetID3Tags(Download dl)
        {
            TagLib.File f = TagLib.File.Create(dl.Filename);
            f.Tag.Album = dl.Podcast;
            f.Tag.Performers = new string[] { dl.Author };
            f.Tag.Year = (uint)dl.Year;
            f.Tag.Genres = new string[] { "Podcast" }; // Genre podcast
            f.Tag.Comment = dl.Description;
            f.Tag.Title = dl.Title;
            if (!string.IsNullOrEmpty(dl.Image))
            {
                string path = string.Format(@"{0}temp\{1}.jpg", "", Guid.NewGuid().ToString());
                byte[] imageBytes;
                using (WebClient client = new WebClient())
                {
                    imageBytes = client.DownloadData(dl.Image);
                }

                TagLib.Id3v2.AttachedPictureFrame cover = new TagLib.Id3v2.AttachedPictureFrame
                {
                    Type = TagLib.PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                    Data = imageBytes,
                    TextEncoding = TagLib.StringType.UTF16
                };

                f.Tag.Pictures = new TagLib.IPicture[] { cover };
            }

            f.Save();
        }

        private static void ShowDlProgress()
        {
            printcounter++;
            lock (ongoing)
            {
                if (printcounter % 400 == 0)
                {
                    Console.Clear();

                    Log($"Downloading to {path}", false);
                    foreach (var dl in ongoing)
                    {
                        var progress = "";

                        for (var i = 0; i < Convert.ToInt32((Math.Floor((decimal)(dl.Percentcomplete / 10)))); i++)
                        {
                            progress += "▓";
                        }
                        for (var i = 0; i < 10 - Convert.ToInt32((Math.Floor((decimal)(dl.Percentcomplete / 10)))); i++)
                        {
                            progress += "░";
                        }

                        Log($"Downloading {dl.Podcast}: {dl.Title} -> {progress} {((dl.Bytescompleted / 1024f) / 1024f).ToString("#0.##")}MB of {((dl.Bytestotal / 1024f) / 1024f).ToString("#0.##")}MB", false);
                    }

                    if (ongoing.Count == 0)
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
