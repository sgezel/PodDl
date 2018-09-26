
# PodDl
Archive podcasts based on RSS feeds or .opml files

Subfolders will be created in the output folder for each podcast downloaded. Files will be save in the format: **\<year\>.\<month>.\<day>-\<episode_title>.mp3**

## Usage examples
- PodDl.exe podcast_list.txt
- PodDl.exe /I:"podcast_export.opml" /O:"D:/Podcasts/" /C:"10"

## Parameters:

 - /I : Path to file containing rss feed urls. This can be either a flat file containing links on each line, or an [.opml](https://en.wikipedia.org/wiki/OPML) file.
 - /O: Specify an output directory. Will create subfolders for each podcast
 - /C: Number of concurrent downloads
