using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnooStream.ViewModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Controls
{
    public class LoadableItemControl : LoadableBaseControl
    {
        ContentControl _loadControl;
        public LoadableItemControl()
        {
            _loadControl = new ContentControl { Visibility = Visibility.Visible, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Content = _loadControl;
        }
        protected override void HandleLoadStateChange(LoadViewModel loadState)
        {
            if (loadState.State != LoadState.Loaded)
            {
                _loadControl.Content = loadState;
                _loadControl.ContentTemplate = TemplateOrDefault(loadState.State);
            }
        }
    }
}
