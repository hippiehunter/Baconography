using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class VotableViewModel : ViewModelBase
    {
        IVotable _votableThing;
        Action _propertyChanged;
        public VotableViewModel(IVotable votableThing, Action propertyChanged)
        {
            _votableThing = votableThing;
            _propertyChanged = propertyChanged;
            originalVoteModifier = (Like ? 1 : 0) + (Dislike ? -1 : 0);
        }

        public void MergeVotable(IVotable votableThing)
        {
            _votableThing = votableThing;
            originalVoteModifier = (Like ? 1 : 0) + (Dislike ? -1 : 0);
            RaisePropertyChanged("Like");
            RaisePropertyChanged("Dislike");
            RaisePropertyChanged("TotalVotes");
            RaisePropertyChanged("LikeStatus");
        }

        public RelayCommand ToggleUpvote { get { return new RelayCommand(ToggleVoteImpl); } }
        public RelayCommand ToggleDownvote { get { return new RelayCommand(ToggleDownvoteImpl); } }
        public RelayCommand ToggleVote { get { return new RelayCommand(ToggleVoteImpl); } }
        private int originalVoteModifier = 0;


        public int TotalVotes
        {
            get
            {
                var currentVoteModifier = (Like ? 1 : 0) + (Dislike ? -1 : 0);
                if (originalVoteModifier == currentVoteModifier)
                    return (_votableThing.Ups - _votableThing.Downs);
                else
                    return (_votableThing.Ups - _votableThing.Downs) + currentVoteModifier;
            }
        }

        public int LikeStatus
        {
            get
            {
                if (Like)
                    return 1;
                if (Dislike)
                    return -1;
                return 0;
            }
        }

        public bool Like
        {
            get
            {
                return _votableThing.Likes ?? false;
            }
            set
            {
                var currentLike = Like;
                if (value)
                    _votableThing.Likes = true;
                else
                    _votableThing.Likes = null;


                if (currentLike != Like)
                {
                    RaisePropertyChanged("Like");
                    RaisePropertyChanged("Dislike");
                    RaisePropertyChanged("TotalVotes");
                    RaisePropertyChanged("LikeStatus");
                }
            }
        }

        public bool Dislike
        {
            get
            {
                return !(_votableThing.Likes ?? true);
            }
            set
            {
                var currentDislike = Dislike;
                if (value)
                    _votableThing.Likes = false;
                else
                    _votableThing.Likes = null;

                if (currentDislike != Dislike)
                {
                    RaisePropertyChanged("Like");
                    RaisePropertyChanged("Dislike");
                    RaisePropertyChanged("TotalVotes");
                    RaisePropertyChanged("LikeStatus");
                }
            }
        }

        private void ToggleVoteImpl()
        {

            int voteDirection = 1;
            if (Like)
            {
                Dislike = true;
                voteDirection = -1;
            }
            else if (Dislike)
            {
                Dislike = false;
                voteDirection = 0;
            }
            else
            {
                Like = true;
            }

            SnooStreamViewModel.RedditService.AddVote(_votableThing.Name, voteDirection);
            _propertyChanged();
        }

        private void ToggleUpvoteImpl()
        {
            int voteDirection = 0;
            if (!Like) //moved to neutral
            {
                voteDirection = 0;
            }
            else
            {
                voteDirection = 1;
            }

            SnooStreamViewModel.RedditService.AddVote(_votableThing.Name, voteDirection);
            _propertyChanged();
        }

        private void ToggleDownvoteImpl()
        {
            int voteDirection = 0;
            if (!Dislike) //moved to neutral
            {
                voteDirection = 0;
            }
            else
            {
                voteDirection = -1;
            }

            SnooStreamViewModel.RedditService.AddVote(_votableThing.Name, voteDirection);
            _propertyChanged();
        }
    }
}
