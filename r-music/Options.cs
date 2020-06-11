using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using RedditSharp.Things;

namespace r_listentothis
{
    public class Options
    {
        public enum ListingType
        {
            All,
            Top,
            Hot,
            New
        }

        [Option('u', "user", Required = true, HelpText = "Reddit account user name.")]
        public string username { get; set; }

        [Option('p', "password", HelpText = "Reddit account password. This is unsafe. If left blank will prompt for password.")]
        public string password { get; set; }

        [Option('r',"subreddit", Default = "/r/listentothis", HelpText ="Subreddit to download songs from. Currently only youtube songs are downloaded.")]
        public string subreddit { get; set; }

        [Option("nsfw", Default = false, HelpText ="Will download NSFW marked songs.")]
        public bool NSFW { get; set; }

        [Option('c',"count",Default = 25, HelpText ="Number of posts to load for each sort.")]
        public int count { get; set; }

        [Option('f',"folder",Default = @".\music", HelpText ="Folder to save files. Default is relative to current directory.")]
        public string folder { get; set; }                

        [Option('s',"sort",Default = ListingType.All, HelpText ="The sorting to use: Top, Hot, New, or All.")]
        public ListingType sort { get; set; }

        [Option('t', "timespan", Default = FromTime.Day, HelpText = "Option for the Top sort: Hour, Day, Week, Month,Year or All")]
        public FromTime timespan { get; set; }

        [Option('v',"video",Default =false, HelpText ="Only download video file, don't convert to audio file. This option saves the video by default.")]
        public bool video { get; set; }

        [Option('s',"save",Default = false, HelpText = "Save the downloaded video after converting to music file instead of deleting.")]
        public bool save { get; set; }

        [Option("comments",Default =2, HelpText ="Comments threshold, posts must be greater than or equal to this number.")]
        public int commentThreshold { get; set; }

        [Option("upvotes", Default =3, HelpText = "Upvotes threshold, posts must be greater than or equal to this number.")]
        public int upvoteThreshold { get; set; }

        //[HelpOption]
        //public string GetUsage()
        //{
        //    return HelpText.AutoBuild(this,
        //      (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        //}
    }
}
