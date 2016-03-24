using SnooStream.Common;
using SnooStream.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    abstract class BaseNavCommand : SnooObservableObject, IHubNavCommand
    {
        public INavigationContext NavigationContext { get; set; }
        public object Glyph { get; set; }
        private bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { Set("IsEnabled", ref _isEnabled, value); } }
        public bool IsInput { get; set; }
        public string Text { get; set; }
        private string _inputText;
        public string InputText { get { return _inputText; } set { Set("InputText", ref _inputText, value); } }
        public abstract void Tapped();
    }

    class SearchNavCommand : BaseNavCommand
    {
        public string TargetSubreddit { get; set; }
        public bool SubredditsOnly { get; set; } = false;

        public SearchNavCommand()
        {
            Text = "Search";
            IsInput = true;
            IsEnabled = true;
            Glyph = "\uE11A";
        }

        public override void Tapped()
        {
            Navigation.GotoSearch(InputText, TargetSubreddit, SubredditsOnly, NavigationContext);
        }
    }

    class RefreshNavCommand : BaseNavCommand
    {
        public IRefreshable Target { get; set; }

        public RefreshNavCommand()
        {
            Text = "Refresh";
            IsInput = false;
            IsEnabled = true;
            Glyph = "\uE117";
        }

        public override void Tapped()
        {
            Target.Refresh();
        }
    }

    class VoteNavCommand : BaseNavCommand
    {
        public VotableViewModel Votable { get; set; }

        public VoteNavCommand()
        {
            //Bind Glyph value to Votable
            IsInput = false;
            IsEnabled = true;
            Text = "Vote";
        }

        public override void Tapped()
        {
            Votable.ToggleVote();
            //Change Glyph to the upvote, neutral or downvote
        }
    }

    class LaunchUri : BaseNavCommand
    {
        public LaunchUri()
        {
            Glyph = "\uE128";
            IsInput = false;
            IsEnabled = true;
            Text = "Browser";
        }

        public override void Tapped()
        {
            NavigationContext.LaunchUri(InputText);
        }
    }

    class PostNavCommand : BaseNavCommand
    {
        public PostNavCommand()
        {
            Glyph = "\uE1D7";
            IsInput = false;
            IsEnabled = true; //this should be based on the users login status
            Text = "Post";
        }
        public override void Tapped()
        {
            throw new NotImplementedException();
        }
    }

    class ReplyNavCommand : BaseNavCommand
    {
        public ReplyNavCommand()
        {
            Glyph = "\uE165";
            IsInput = false;
            IsEnabled = true; //this should be based on the users login status
            Text = "Reply";
        }
        public override void Tapped()
        {
            throw new NotImplementedException();
        }
    }

    class AboutSubredditNavCommand : BaseNavCommand
    {
        public string TargetSubreddit { get; set; }
        public AboutSubredditNavCommand()
        {
            Glyph = "\uE150";
            IsInput = false;
            IsEnabled = true; 
            Text = "Sidebar";
        }

        public override void Tapped()
        {
            throw new NotImplementedException();
        }
    }
}
