using MediaToolkit;
using MediaToolkit.Model;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using YoutubeExtractor;
using CommandLine;

namespace r_listentothis
{
    public class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();
            Program program = new Program();
            int hasError = 0;

            var result = CommandLine.Parser.Default.ParseArguments<Options>(args)
                            .MapResult(
                                opts => { options = opts; return 0; }, //in case parser sucess
                                errs => { hasError = 1; return 0; }
                             ); //in  case parser fail

            if(hasError == 1) { return; }

            if(string.IsNullOrEmpty(options.password))
            {
                Console.WriteLine("Password:");
                options.password = program.ReadPassword();
            }

            //get top, hot and new from previous monday (1 week, building up through the week)
            //currently top by day and each listing limited by 50
            var stopwatchAll = program.Start();
            program.ValidateFolder(options);

            //generate paths and link pairs
            var stopwatchGetPosts = program.Start();
            Console.WriteLine("Getting Reddit Posts:");
            var posts = program.GetRedditPosts(options).ToList();
            Console.WriteLine($"{posts.Count()} in time: {program.Stop(stopwatchGetPosts)}");
            program.RemoveDuplicatePosts(posts, options);
            Console.WriteLine();
            Console.WriteLine($"Downloading {posts.Count} posts:");
            program.DownloadPosts(posts, options);
            Console.WriteLine($"Processed all current videos, time: {program.Stop(stopwatchAll)}");
            Console.WriteLine("press any key to finish");
            Console.ReadKey();
        }

        public virtual bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }

        public int RemoveDuplicatePosts(List<Post> posts, Options options)
        {
            //see if path exists (discard if so)
            var removeCount = posts.RemoveAll(x => Exists(GenerateSavePath(x.Title, options.folder, "mp3")));
            if (removeCount > 0)
                Console.WriteLine($"{removeCount} already downloaded");
            return removeCount;
        }

        private void DownloadPosts(List<Post> posts, Options options)
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
                    if (!Exists(saveFilePath))
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

        private void ValidateFolder(Options options)
        {
            if (!Directory.Exists(options.folder))
            {
                Directory.CreateDirectory(options.folder);
                Console.WriteLine($"created music folder at {options.folder}");
            }
        }

        public string GenerateSavePath(string name, string folder, string extension)
        {
            var saveFilePath = Path.Combine(folder, CleanFileName(name));
            saveFilePath = Path.ChangeExtension(saveFilePath, extension);
            return saveFilePath;
        }

        private List<Post> GetRedditPosts(Options options)
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
            foreach (var listing in listings)
            {
                var selected = listing.Take(options.count)
                    .Where(x => (x.CommentCount >= options.commentThreshold && x.Upvotes >= options.upvoteThreshold && x.NSFW == options.NSFW) || x.Liked == true)
                    .Where(x => DownloadUrlResolver.TryNormalizeYoutubeUrl(x.Url.ToString(), out _));

                posts.AddRange(selected);
                Console.Write("=");
            }
            Console.WriteLine();

            posts = posts.GroupBy(x => x.Id).Select(g => g.First()).ToList();
            return posts;
        }

        private void ConvertToMP3(string outputFile)
        {
            var input = new MediaFile { Filename = outputFile };
            var output = new MediaFile { Filename = System.IO.Path.ChangeExtension(outputFile, "mp3") };

            using (var engine = new Engine())
            {
                engine.GetMetadata(input);
                engine.Convert(input, output);
            }
        }

        private void DownloadVideo(VideoInfo video, string path)
        {            
            var videoDownloader = new VideoDownloader(video, path);
            videoDownloader.Execute();
        }

        private VideoInfo GetBestAudioVideo(string url)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);

            VideoInfo video = videoInfos
                .OrderByDescending(info => info.AudioBitrate)
                .First();

            return video;
        }

        System.Diagnostics.Stopwatch Start()
        {
            return System.Diagnostics.Stopwatch.StartNew();
        }

        string Stop(System.Diagnostics.Stopwatch sw)
        {
            sw.Stop();
            return $"{(sw.ElapsedMilliseconds / 1000.0):F}s";
        }

        string CleanFileName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public string ReadPassword()
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

        private void HandleBackspace(Stack<string> passbits)
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
