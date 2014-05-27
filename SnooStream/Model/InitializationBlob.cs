using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class InitializationBlob
    {
        public Dictionary<string, string> Settings { get; set; }
        public Dictionary<string, bool> NSFWFilter { get; set; }
        public UserState DefaultUser { get; set; }
        public SelfInit Self {get; set;}
        public SubredditRiverInit Subreddits {get; set;}
        //original linkid, url, date first seen
        public List<Tuple<string, string, DateTime>> LockscreenImages { get; set; }
        public List<Tuple<string, string, DateTime>> LiveTileImages { get; set; }
		public string NavigationBlob { get; set; }
    }

    public class SelfInit
    {
        public string AfterSelfMessage { get; set; }
        public string AfterSelfSentMessage { get; set; }
        public string AfterSelfAction { get; set; }
        public List<Thing> SelfThings { get; set; }
    }

    public class SubredditRiverInit
    {
        public List<SubredditInit> Pinned { get; set; }
        public List<SubredditInit> Subscribed { get; set; }
    }

    public class SubredditInit
    {
        public Subreddit Thing { get; set; }
        public List<Link> Links { get; set; }
        public string DefaultSort { get; set; }
    }
}
