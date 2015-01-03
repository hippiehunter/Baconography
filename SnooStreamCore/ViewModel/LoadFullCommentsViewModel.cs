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
            _context = context;
        }

        public async void LoadFully()
        {
            await _context.LoadAndMergeFull(false);
        }
    }
}
