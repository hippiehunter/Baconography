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
    public class LoadableControl : UserControl
    {
        private ContentControl _loadControl;
        private ContentControl _realContent;
        private Grid _containerGrid;
        private bool _initiallyBound = false;
        private Dictionary<LoadState, DataTemplate> _templateLookup = new Dictionary<LoadState, DataTemplate>();
        public DataTemplate LoadedContentTemplate { get { return TemplateOrDefault(LoadState.Loaded); } set { UpdateTemplate(LoadState.Loaded, value); } }
        public DataTemplate LoadingContentTemplate { get { return TemplateOrDefault(LoadState.Loading); } set { UpdateTemplate(LoadState.Loading, value); } }
        public DataTemplate CancelledContentTemplate { get { return TemplateOrDefault(LoadState.Cancelled); } set { UpdateTemplate(LoadState.Cancelled, value); } }
        public DataTemplate NoItemsContentTemplate { get { return TemplateOrDefault(LoadState.NoItems); } set { UpdateTemplate(LoadState.NoItems, value); } }
        public DataTemplate NotFoundContentTemplate { get { return TemplateOrDefault(LoadState.NotFound); } set { UpdateTemplate(LoadState.NotFound, value); } }
        public DataTemplate DisallowedContentTemplate { get { return TemplateOrDefault(LoadState.Disallowed); } set { UpdateTemplate(LoadState.Disallowed, value); } }
        public DataTemplate NetworkFailureContentTemplate { get { return TemplateOrDefault(LoadState.NetworkFailure); } set { UpdateTemplate(LoadState.NetworkFailure, value); } }
        public DataTemplate NetworkCapturedContentTemplate { get { return TemplateOrDefault(LoadState.NetworkCaptured); } set { UpdateTemplate(LoadState.NetworkCaptured, value); } }
        public DataTemplate FailureContentTemplate { get { return TemplateOrDefault(LoadState.Failure); } set { UpdateTemplate(LoadState.Failure, value); } }
        public DataTemplate NotAuthorizedContentTemplate { get { return TemplateOrDefault(LoadState.NotAuthorized); } set { UpdateTemplate(LoadState.NotAuthorized, value); } }

        private void UpdateTemplate(LoadState state, DataTemplate template)
        {
            if (!_templateLookup.ContainsKey(state))
                _templateLookup.Add(state, template);
            else
                _templateLookup[state] = template;

            if (state == LoadState.Failure)
            {
                UpdateMissingTemplate(LoadState.None, template);
                UpdateMissingTemplate(LoadState.Cancelled, template);
                UpdateMissingTemplate(LoadState.NoItems, template);
                UpdateMissingTemplate(LoadState.NotFound, template);
                UpdateMissingTemplate(LoadState.Disallowed, template);
                UpdateMissingTemplate(LoadState.NetworkFailure, template);
                UpdateMissingTemplate(LoadState.NetworkCaptured, template);
                UpdateMissingTemplate(LoadState.NotAuthorized, template);
            }

            //need to reprocess the content templates in case we have already been bound and just changed everything
            if (DataContext is IHasLoadableState)
            {
                HandleLoadStateChange(((IHasLoadableState)DataContext).LoadState);
            }
        }

        private void UpdateMissingTemplate(LoadState state, DataTemplate template)
        {
            if (!_templateLookup.ContainsKey(state))
                _templateLookup.Add(state, template);
        }

        private DataTemplate TemplateOrDefault(LoadState state)
        {
            DataTemplate value;
            if (_templateLookup.TryGetValue(state, out value))
                return value;
            else if (state == LoadState.Loading)
                return null;
            else if (state == LoadState.Failure)
                return null;
            else
                return TemplateOrDefault(LoadState.Failure);
        }

        public LoadableControl()
        {
            DataContextChanged += LoadableControl_DataContextChanged;
            _loadControl = new ContentControl { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            _realContent = new ContentControl { Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Content = _containerGrid = new Grid();
            _containerGrid.Children.Add(_loadControl);
            _containerGrid.Children.Add(_realContent);
        }

        private void LoadableControl_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
        {
            if (DataContext != args.NewValue || !_initiallyBound)
            {
                if (DataContext is IHasLoadableState && _initiallyBound)
                {
                    ((IHasLoadableState)DataContext).LoadState.PropertyChanged -= LoadState_PropertyChanged;
                }
                //register and unregister all things load state, make sure the content controls stay valid as long as possible so we dont pay for making new ones
                if (args.NewValue is IHasLoadableState)
                {
                    HandleLoadStateChange((args.NewValue as IHasLoadableState).LoadState);
                    ((IHasLoadableState)args.NewValue).LoadState.PropertyChanged += LoadState_PropertyChanged;
                }

                _initiallyBound = true;
            }
        }

        private void HandleLoadStateChange(LoadViewModel loadState)
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

        private void LoadState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State" && sender is LoadViewModel)
            {
                HandleLoadStateChange(sender as LoadViewModel);
            }
        }
    }
}
