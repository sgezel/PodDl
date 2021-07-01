using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace PodDl
{
    /// <summary>
    /// PodDL XML reader
    /// </summary>
    class PodDlXmlReader : XmlTextReader
    {
        //
        // Constants
        //
        /// <summary>
        /// Custom UTC Date/Time format
        /// 
        /// e.g. Wed Oct 07 08:00:07 GMT 2009
        /// </summary>
        private const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z yyyy";

        //
        // Locals
        //

        /// <summary>
        /// Whether or not we're reading the enclosure
        /// </summary>
        private bool readingEnclosure = false;

        //
        // Constructors
        //
        /// <summary>
        /// Creates a new instance of the PodDlXmlReader with a Stream
        /// </summary>
        /// <param name="s">Stream</param>
        public PodDlXmlReader(Stream s) : base(s) { }

        /// <summary>
        /// Creates a new instance of the PodDlXmlReader with an input URI
        /// </summary>
        /// <param name="inputUri">Input URI</param>
        public PodDlXmlReader(string inputUri) : base(inputUri) { }

        //
        // Overrides
        //
        public override void ReadStartElement()
        {
            if (string.Equals(base.NamespaceURI, string.Empty, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(base.LocalName, "enclosure", StringComparison.InvariantCultureIgnoreCase))
            {
                readingEnclosure = true;
            }
            base.ReadStartElement();
        }

        public override void ReadEndElement()
        {
            if (readingEnclosure)
            {
                readingEnclosure = false;
            }

            base.ReadEndElement();
        }

        public override string ReadString()
        {
            // if we're reading an enclosure, do our own Date/Time parsing
            if (readingEnclosure)
            {
                // read the date as a string
                string dateString = base.ReadString();

                // try to parse it
                if (!DateTime.TryParse(dateString, out DateTime dt))
                {
                    dt = DateTime.ParseExact(dateString, CustomUtcDateTimeFormat, CultureInfo.InvariantCulture);
                }

                // convert to Universal time
                return dt.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
            }
            else
            {
                return base.ReadString();
            }
        }

        public override bool ReadAttributeValue()
        {
            // if length="none", skip it
            if (base.LocalName.Equals("length", StringComparison.InvariantCultureIgnoreCase) &&
                base.Value.Equals("none", StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }

            return base.ReadAttributeValue();
        }

        public override string ReadElementContentAsString (string localName, string namespaceURI)
        {
            return base.ReadElementContentAsString(localName, namespaceURI);
        }
    }
}
