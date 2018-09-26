using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodDl
{
    class Download
    {
        public string Id { get; set; }
        public string Podcast { get; set; }
        public Uri Url { get; set; }
        public string Title { get; set; }
        public int Percentcomplete { get; set; }

        public long Bytescompleted { get; set; }

        public long Bytestotal { get; set; }

        public string Filename { get; set; }
    }
}
