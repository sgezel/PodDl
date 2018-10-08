using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PodDl
{
    public class Documentation
    {
        private Assembly assembly = Assembly.GetExecutingAssembly();

        public List<Podcast> Podcasts { get; set; }

        public string GenerateIndex()
        {
            var indexResource = "PodDl.Documentation.index.html";

            string index = "";
            using (Stream stream = assembly.GetManifestResourceStream(indexResource))
            using (StreamReader reader = new StreamReader(stream))
            {
                index = reader.ReadToEnd();
            }

            index = index.Replace("[podcast_block]", GeneratePodcastBlocks());

            return index;
        }

        public string GeneratePodcastDetails(Podcast podcast)
        {
            var episodesResource = "PodDl.Documentation.episodes.html";

            string details = "";
            using (Stream stream = assembly.GetManifestResourceStream(episodesResource))
            using (StreamReader reader = new StreamReader(stream))
            {
                details = reader.ReadToEnd();
            }

            details = details.Replace("[Episodes]", GenerateEpisodeBlock(podcast.Episodes));

            return details;
        }

        public string GeneratePodcastBlocks()
        {
            var output = "";
            var podcastBlockResource = "PodDl.Documentation.podcast.html";

            string podcastBlock = "";
            using (Stream stream = assembly.GetManifestResourceStream(podcastBlockResource))
            using (StreamReader reader = new StreamReader(stream))
            {
                podcastBlock = reader.ReadToEnd();
            }

            foreach (var podcast in Podcasts)
            {
                output += podcastBlock;

                output = Fill(output, "Title", podcast.Title);
                output = Fill(output, "Image", podcast.Image);
                output = Fill(output, "Description", podcast.Description);
                output = Fill(output, "Url", podcast.Url);
                output = Fill(output, "Author", podcast.Author);
                output = Fill(output, "Episode_count", podcast.Episodes.Count.ToString());
                output = Fill(output, "Detail", $"_documentation/{podcast.DetailFileName}.html");

            }

            return output;
        }

        public string GenerateEpisodeBlock(List<Episode> episodes)
        {
            var output = "";
            var episodeBlockResource = "PodDl.Documentation.episode.html";

            string episodeBlock = "";
            using (Stream stream = assembly.GetManifestResourceStream(episodeBlockResource))
            using (StreamReader reader = new StreamReader(stream))
            {
                episodeBlock = reader.ReadToEnd();
            }

            episodes.Reverse();

            foreach (var episode in episodes)
            {
                output += episodeBlock;

                output = Fill(output, "Title", episode.Title);
                output = Fill(output, "Description", episode.Description);
                output = Fill(output, "Duration", episode.Duration);
                output = Fill(output, "Img", episode.Image);
                output = Fill(output, "File", episode.Url);

            }

            return output;
        }

        private string Fill(string str, string o, string n)
        {
            return str.Replace($"[{o}]", n);
        }
    }

    public class Podcast
    {
        public List<Episode> Episodes {get; set;}

        public string Title { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string DetailFileName { get; set; }
        public string Author { get; set; }
    }

    public class Episode
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
    }
}
