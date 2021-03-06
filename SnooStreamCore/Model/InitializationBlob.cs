﻿using SnooSharp;
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
        public SubredditRiverInit Subreddits {get; set;}
		public string NavigationBlob { get; set; }
    }

    public class SelfInit
    {
		public DateTime? LastRefresh { get; set; }
        public string AfterSelfMessage { get; set; }
        public string AfterSelfSentMessage { get; set; }
        public string AfterSelfAction { get; set; }
        public List<Thing> SelfThings { get; set; }
		public List<Thing> ModThings { get; set; }
		public List<string> DisabledModeration { get; set; }
    }

    public class SubredditRiverInit
    {
        public List<SubredditInit> Local { get; set; }
    }

    public class SubredditInit
    {
        public Subreddit Thing { get; set; }
        public string DefaultSort { get; set; }
		public DateTime? LastRefresh { get; set; }
        public string Category { get; set; }
    }
}
