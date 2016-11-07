using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Controls
{
    /// <summary>
    /// Needs at least LoadedContentTemplate,FailureContentTemplate,LoadingContentTemplate to be set
    /// </summary>
    public class LoadableControl : LoadableBaseControl
    {
        private ContentControl _loadControl;
        private ContentControl _realContent;
        private Grid _containerGrid;
        private bool _initiallyBound = false;

        public LoadableControl()
        {
            _loadControl = new ContentControl { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            _realContent = new ContentControl { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Content = _containerGrid = new Grid();
            _containerGrid.Children.Add(_loadControl);
            _containerGrid.Children.Add(_realContent);
        }

        protected override void HandleLoadStateChange(LoadViewModel loadState)
        {
            if (loadState.State == LoadState.Loaded)
            {
                if (!(_realContent.Content == DataContext &&
                    _realContent.ContentTemplate == TemplateOrDefault(loadState.State) &&
                    _realContent.Visibility == Visibility.Visible))
                {
                    _realContent.Content = DataContext;
                    _realContent.ContentTemplate = TemplateOrDefault(loadState.State);
                    _loadControl.Content = null;
                    _loadControl.Visibility = Visibility.Collapsed;
                    _realContent.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _realContent.Content = null;
                _realContent.Visibility = Visibility.Collapsed;
                _loadControl.Visibility = Visibility.Visible;
                _loadControl.Content = loadState;
                _loadControl.ContentTemplate = TemplateOrDefault(loadState.State);
            }
        }
    }
}
