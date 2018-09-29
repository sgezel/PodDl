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

        public string Season { get; set; }
        public string Episode { get; set; }

        public string Image { get; set; }

        public int Year { get; set; }
        //Fill ID3 Tags
        public string Author { get; set; }
        public string Description { get; set; }
        

    }
}
