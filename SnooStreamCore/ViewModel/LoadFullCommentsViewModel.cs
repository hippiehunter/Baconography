using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnooStream.ViewModel
{
    public class LoadFullCommentsViewModel : ViewModelBase
    {
        CommentsViewModel _context;
        public LoadFullCommentsViewModel(CommentsViewModel context)
        {
            Load = new RelayCommand(LoadFully);
            _context = context;
        }

        public RelayCommand Load { get; set; }

        public async void LoadFully()
        {
            await _context.LoadAndMergeFull(false);
        }
    }
}
