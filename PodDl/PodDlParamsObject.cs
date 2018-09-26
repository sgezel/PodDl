using ConsoleCommon;
using ConsoleCommon.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodDl
{
    class PodDlParamsObject : ParamsObject
    {
        public PodDlParamsObject(string[] args)
            : base(args)
        {

        }
        
        [Switch("I", true, 0, false)]
        [SwitchHelpText("Path to file containing rss feed urls. This can be either a flat file containing links on each line, or an .opml file")]
        public string InputPath { get; set; }
        
        [Switch("O", false, 1, false)]
        [SwitchHelpText("Directory where podcasts will be stored")]
        public string OutputPath { get; set; }

        [Switch("C", false, 2, false)]
        [SwitchHelpText("# of Consecutive download connections")]
        public int Connections { get; set; }


        public override Dictionary<Func<bool>, string> GetParamExceptionDictionary()
        {
            Dictionary<Func<bool>, string> _exceptionChecks = new Dictionary<Func<bool>, string>();

            /*Func<bool> OutputExistst = new Func<bool>(() => Directory.Exists(this.OutputPath));
            Func<bool> InputExistst = new Func<bool>(() => File.Exists(this.InputPath));


            _exceptionChecks.Add(InputExistst,
                                 "Please enter a valid input file.");
            _exceptionChecks.Add(OutputExistst,
                                 "Please enter a valid output directory.");*/

            return _exceptionChecks;
        }


        [HelpText(0)]
        public string Description
        {
            get { return "Archive podcasts based on RSS feeds or .opml files"; }
        }


        [HelpText(1)]
        public override string Usage
        {
            get { return base.Usage; }
        }

        [HelpText(2, "Parameters")]
        public override string SwitchHelp
        {
            get { return base.SwitchHelp; }
        }

        public override List<string> HelpCommands
        {
            get
            {
                return new List<string> { "/?", "help", "/help", "h", "/h", "-help", "-h" };
            }
        }

        public override SwitchOptions Options
        {
            get
            {
                return new SwitchOptions(switchStartChars: new List<char> { '-', '/' },
                switchEndChars: new List<char> { ':', '-' },
                                            switchNameRegex: "[_A-Za-z]+[_A-Za-z0-9]*");
            }
        }
    }
}

