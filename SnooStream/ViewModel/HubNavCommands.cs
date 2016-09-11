using SnooStream.Common;
using SnooStream.Controls;
using SnooStream.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.ViewModel
{
    abstract class BaseNavCommand : SnooObservableObject, IHubNavCommand
    {
        public INavigationContext NavigationContext { get; set; }
        private object _glyph;
        public object Glyph { get { return _glyph; } set { Set("Glyph", ref _glyph, value); } }
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

    abstract class CollectionViewNavCommand : BaseNavCommand
    {
        public Func<object, bool> CheckEnabled { get; set; }
        public CollectionViewNavCommand(Func<object, bool> checkEnabled, ICollectionView collectionView)
        {
            CheckEnabled = checkEnabled;
            collectionView.CurrentChanged += CollectionView_CurrentChanged;
            IsEnabled = collectionView.CurrentItem != null ? CheckEnabled(collectionView.CurrentItem) : false;
        }

        private void CollectionView_CurrentChanged(object sender, object e)
        {
            IsEnabled = e != null ? CheckEnabled(e) : false;
        }
    }

    class DelayedRefreshNavCommand : CollectionViewNavCommand
    {
        public Func<IRefreshable> Target { get; set; }

        public DelayedRefreshNavCommand(Func<IRefreshable> target, Func<object, bool> checkEnabled, ICollectionView collectionView) :
            base(checkEnabled, collectionView)
        {
            Target = target;
            Text = "Refresh";
            IsInput = false;
            Glyph = "\uE117";
        }

        public override void Tapped()
        {
            Target()?.Refresh();
        }
    }

    class DelayedGalleryCommand : CollectionViewNavCommand
    {
        public Action Target { get; set; }

        public DelayedGalleryCommand(Action target, Func<object, bool> checkEnabled, ICollectionView collectionView) :
            base(checkEnabled, collectionView)
        {
            Target = target;
            Text = "Gallery";
            IsInput = false;
            Glyph = "\uE138";
        }

        public override void Tapped()
        {
            Target();
        }
    }

    class VoteNavCommand : BaseNavCommand
    {
        public VotableViewModel Votable { get; set; }

        public VoteNavCommand(VotableViewModel votable)
        {
            Votable = votable;
            //Bind Glyph value to Votable
            Glyph = new VoteUriConverter().Convert(votable, null, null, null);
            IsInput = false;
            IsEnabled = true;
            Text = "Vote";
        }

        public override void Tapped()
        {
            Votable.ToggleVote();
            //Change Glyph to the upvote, neutral or downvote
            Glyph = new VoteUriConverter().Convert(Votable, null, null, null);
            
        }
    }

    class DelayedVoteNavCommand : BaseNavCommand
    {
        public Func<VotableViewModel> Votable { get; set; }

        public DelayedVoteNavCommand(Func<VotableViewModel> votable)
        {
            Votable = votable;
            //Bind Glyph value to Votable
            Glyph = new VoteUriConverter().Convert(votable(), null, null, null);
            IsInput = false;
            IsEnabled = true;
            Text = "Vote";
        }

        public override void Tapped()
        {
            Votable().ToggleVote();
            //Change Glyph to the upvote, neutral or downvote
            Glyph = new VoteUriConverter().Convert(Votable(), null, null, null);
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

    class DelayedLaunchUri : BaseNavCommand
    {
        public Func<string> TargetUrl { get; set; }
        public DelayedLaunchUri()
        {
            Glyph = "\uE128";
            IsInput = false;
            IsEnabled = true;
            Text = "Browser";
        }

        public override void Tapped()
        {
            NavigationContext.LaunchUri(TargetUrl());
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
