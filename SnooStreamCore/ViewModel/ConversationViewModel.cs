using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ConversationViewModel : ViewModelBase
    {
        SelfStreamViewModel _selfStream;
        public ConversationViewModel(ActivityGroupViewModel targetGroup, SelfStreamViewModel selfStream, CreateMessageViewModel replyMessage = null)
        {
            _selfStream = selfStream;
            CurrentGroup = targetGroup;
            if (replyMessage == null)
                IsEditing = false;
            else
            {
                IsEditing = true;
                Reply = replyMessage;
            }

            if (CurrentGroup == null)
            {
                CurrentGroup = new ActivityGroupViewModel("");
            }

            GotoReply = new RelayCommand(() =>
            {
                
                IsEditing = true;
				string targetUser = ActivityViewModel.GetAuthor(CurrentGroup.FirstActivity);
				if (targetUser == SnooStreamViewModel.RedditUserState.Username)
				{
					var betterActivity = ((IEnumerable<ActivityViewModel>)CurrentGroup.Activities).FirstOrDefault(vm => ActivityViewModel.GetAuthor(vm) != SnooStreamViewModel.RedditUserState.Username);
					if (betterActivity != null)
						targetUser = ActivityViewModel.GetAuthor(betterActivity);
				}
				Reply = new CreateMessageViewModel { Username = targetUser, Topic = CurrentGroup.FirstActivity.PreviewTitle, IsReply = true };
                RaisePropertyChanged("Reply");
                RaisePropertyChanged("IsEditing");
            });

            DeleteMessage = new RelayCommand(() =>
            {

            });

            GotoNewer = new RelayCommand(() =>
            {

            });

            GotoOlder = new RelayCommand(() =>
            {

            });
        }

        public ActivityGroupViewModel CurrentGroup { get; set; }
        public bool IsEditing { get; set; }
        public CreateMessageViewModel Reply { get; set; }
        public RelayCommand GotoReply { get; set; }
        public RelayCommand DeleteMessage { get; set; }
        public RelayCommand GotoNewer { get; set; }
        public RelayCommand GotoOlder { get; set; }
    }
}
