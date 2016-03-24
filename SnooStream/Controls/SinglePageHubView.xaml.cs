using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SnooStream.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SinglePageHubView : Page
    {
        public SinglePageHubView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var hubNavItem = e.Parameter as HubNavItem;
            DataContext = hubNavItem;
            if (hubNavItem.Content is IHasHubNavCommands)
            {
                var symbol = new FontFamily("Segoe UI Symbol");
                var commandBar = new CommandBar();
                var commands = ((IHasHubNavCommands)hubNavItem.Content).Commands;
                foreach (var command in commands)
                {

                    var icon = command.Glyph is string ? new FontIcon { FontFamily = symbol, Glyph = command.Glyph as string } : command.Glyph as IconElement;

                    var madeCommand = new AppBarButton
                    {
                        Icon = icon,
                        Label = command.Text,
                        IsEnabled = command.IsEnabled
                    };

                    madeCommand.Command = new GalaSoft.MvvmLight.Command.RelayCommand(command.Tapped);
                    var isEnabledBinding = new Binding { Path = new PropertyPath("IsEnabled"), Mode = BindingMode.OneWay, Source = command };
                    madeCommand.SetBinding(AppBarButton.IsEnabledProperty, isEnabledBinding);
                    commandBar.PrimaryCommands.Add(madeCommand);

                }
                BottomAppBar = commandBar;
            }
        }
    }
}
