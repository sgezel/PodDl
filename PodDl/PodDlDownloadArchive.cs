using System.Collections.Generic;
using System.IO;

namespace PodDl
{
    /// <summary>
    /// Database of downloaded podcast episodes (based on item ID)
    /// </summary>
    class PodDlDownloadArchive
    {
        /// <summary>
        ///  Database of title-IDs
        /// </summary>
        private readonly HashSet<string> _archived = new HashSet<string>();

        /// <summary>
        /// Archive file
        /// </summary>
        private readonly string _file = @"";

        /// <summary>
        /// Creates a new instance of a PodDlDownloadArchive 
        /// </summary>
        /// <param name="file">Database file path</param>
        public PodDlDownloadArchive(string file)
        {
            _file = file;

            if (!string.IsNullOrEmpty(_file))
            {
                if (File.Exists(_file))
                {
                    string[] contents = File.ReadAllLines(_file);

                    // each line is a title-ID
                    foreach (string id in contents)
                    {
                        _archived.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether or not the Database has the specified title and ID
        /// </summary>
        /// <param name="title">Podcast title</param>
        /// <param name="id">Podcast item ID</param>
        /// <returns>True if the database has the specified title and ID</returns>
        public bool HasId(string title, string id)
        {
            return _archived.Contains(GetId(title, id));
        }

        /// <summary>
        /// Adds the specified title and ID to the database
        /// </summary>
        /// <param name="title">Podcast title</param>
        /// <param name="id">Podcast item ID</param>
        public void Add(string title, string id)
        {
            if (!HasId(title, id))
            {
                _archived.Add(GetId(title, id));
            }
        }

        /// <summary>
        /// Saves the database
        /// </summary>
        public void Save()
        {
            File.WriteAllLines(_file, _archived);
        }

        /// <summary>
        /// Gets the unique ID for the title/ID combination
        /// </summary>
        /// <param name="title">Podcast title</param>
        /// <param name="id">Podcast item ID</param>
        /// <returns>String ID</returns>
        private string GetId(string title, string id)
        {
            return $"{title}-{id}";
        }
    }
}
