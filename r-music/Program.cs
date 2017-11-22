using MediaToolkit;
using MediaToolkit.Model;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using YoutubeExtractor;

namespace r_listentothis
{
    class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.ReadKey();
                return;
            }

            if(string.IsNullOrEmpty(options.password))
            {
                Console.WriteLine("Password:");
                options.password = ReadPassword();
            }

            //get top, hot and new from previous monday (1 week, building up through the week)
            //currently top by day and each listing limited by 50
            var stopwatchAll = Start();
            ValidateFolder(options);

            //generate paths and link pairs
            var stopwatchGetPosts = Start();
            Console.WriteLine("Getting Reddit Posts:");
            var posts = GetRedditPosts(options).ToList();
            Console.WriteLine($"{posts.Count()} in time: {Stop(stopwatchGetPosts)}");
            RemoveDuplicatePosts(posts, options);
            Console.WriteLine();
            Console.WriteLine($"Downloading {posts.Count} posts:");
            DownloadPosts(posts, options);
            Console.WriteLine($"Processed all current videos, time: {Stop(stopwatchAll)}");
            Console.WriteLine("press any key to finish");
            Console.ReadKey();
        }

        private static void RemoveDuplicatePosts(List<Post> posts, Options options)
        {
            //see if path exists (discard if so)
            var removeCount = posts.RemoveAll(x => File.Exists(GenerateSavePath(x.Title, options.folder, "mp3")));
            if (removeCount > 0)
                Console.WriteLine($"{removeCount} already downloaded");
        }

        private static void DownloadPosts(List<Post> posts, Options options)
        {
            foreach (var post in posts)
            {
                try
                {
                    var stopwatchProcess = Start();
                    Console.WriteLine(post.Title);
                    Console.Write("---> ");

                    Console.Write("Info ");
                    var video = GetBestAudioVideo(post.Url.ToString());
                    var saveFilePath = GenerateSavePath(post.Title, options.folder, video.VideoExtension);
                    if (!File.Exists(saveFilePath))
                    {
                        //Download YouTube (working), could we download multiple videos at a time?
                        Console.Write("Downloading ");
                        DownloadVideo(video, saveFilePath);
                    }
                    else
                        Console.Write("(Already Downloaded) ");

                    if (!options.video)
                    {
                        //Convert to MP3 (working)
                        Console.Write("Converting ");
                        ConvertToMP3(saveFilePath);
                    }

                    if (!options.video && !options.save)
                    {
                        Console.Write("Deleting ");
                        //Delete video
                        File.Delete(saveFilePath);
                    }
                    Console.Write($"({Stop(stopwatchProcess)})");
                    Console.WriteLine();
                }
                catch (Exception x)
                {
                    Console.WriteLine();
                    Console.WriteLine($"WARNING: {x.Message}");
                }
            }
        }

        private static void ValidateFolder(Options options)
        {
            if (!Directory.Exists(options.folder))
            {
                Directory.CreateDirectory(options.folder);
                Console.WriteLine($"created music folder at {options.folder}");
            }
        }

        public static string GenerateSavePath(string name, string folder, string extension)
        {
            var saveFilePath = Path.Combine(folder, CleanFileName(name));
            saveFilePath = Path.ChangeExtension(saveFilePath, extension);
            return saveFilePath;
        }

        private static List<Post> GetRedditPosts(Options options)
        {
            //reddit show top vidoes for week            
            var reddit = new Reddit(options.username, options.password);
            var subreddit = reddit.GetSubreddit(options.subreddit);
            var listings = new List<Listing<Post>>();

            switch (options.sort)
            {
                case Options.ListingType.Hot:
                    listings.Add(subreddit.Hot);
                    break;
                case Options.ListingType.New:
                    listings.Add(subreddit.New);
                    break;
                case Options.ListingType.Top:
                    listings.Add(subreddit.GetTop(options.timespan));
                    break;
                case Options.ListingType.All:
                    listings.Add(subreddit.GetTop(options.timespan));
                    listings.Add(subreddit.Hot);
                    listings.Add(subreddit.New);
                    break;
            }

            var progress = "";
            for (int i = 0; i < listings.Count(); i++) { progress += "-"; }
            
            Console.WriteLine(progress);
            var posts = new List<Post>();
            foreach (var l in listings)
            {
                var selected = l.Take(options.count)
                    .Where(x => (x.CommentCount >= options.commentThreshold && x.Upvotes >= options.upvoteThreshold && x.NSFW == options.NSFW) || x.Liked == true)
                    .Where(x => DownloadUrlResolver.TryNormalizeYoutubeUrl(x.Url.ToString(), out _));

                posts.AddRange(selected);
                Console.Write("=");
            }
            Console.WriteLine();

            posts = posts.GroupBy(x => x.Id).Select(g => g.First()).ToList();
            return posts;
        }

        private static void ConvertToMP3(string outputFile)
        {
            var input = new MediaFile { Filename = outputFile };
            var output = new MediaFile { Filename = System.IO.Path.ChangeExtension(outputFile, "mp3") };

            using (var engine = new Engine())
            {
                engine.GetMetadata(input);
                engine.Convert(input, output);
            }
        }

        private static void DownloadVideo(VideoInfo video, string path)
        {            
            var videoDownloader = new VideoDownloader(video, path);
            videoDownloader.Execute();
        }

        private static VideoInfo GetBestAudioVideo(string url)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);

            VideoInfo video = videoInfos
                .OrderByDescending(info => info.AudioBitrate)
                .First();

            return video;
        }

        static System.Diagnostics.Stopwatch Start()
        {
            return System.Diagnostics.Stopwatch.StartNew();
        }

        static string Stop(System.Diagnostics.Stopwatch sw)
        {
            sw.Stop();
            return $"{(sw.ElapsedMilliseconds / 1000.0):F}s";
        }

        static string CleanFileName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public static string ReadPassword()
        {
            var passbits = new Stack<string>();
            //keep reading
            for (var cki = Console.ReadKey(true); cki.Key != ConsoleKey.Enter; cki = Console.ReadKey(true))
            {
                if (cki.Key == ConsoleKey.Backspace)
                {
                    HandleBackspace(passbits);
                }
                else
                {
                    HandleValidKeyPress(passbits, cki);
                }
            }
            string[] pass = passbits.ToArray();
            Array.Reverse(pass);
            Console.Write(Environment.NewLine);
            return string.Join(string.Empty, pass);
        }

        private static void HandleValidKeyPress(Stack<string> passbits, ConsoleKeyInfo cki)
        {
            Console.Write("*");
            passbits.Push(cki.KeyChar.ToString());
        }

        private static void HandleBackspace(Stack<string> passbits)
        {
            if (passbits.Any())
            {
                //rollback the cursor and write a space so it looks backspaced to the user
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                Console.Write(" ");
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                passbits.Pop();
            }
        }
    }
}
