using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System.Windows.Input;

namespace SnooStreamWP8.View.Controls
{
    public partial class LargeTextEditor : UserControl
    {
        private static readonly string idleDetectJavaScript =
@"(function() {
    var timeout;
    var isUpdated = false;

    function hideOnIdle() {
        if (timeout) {
            window.clearTimeout(timeout);
        }
        timeout = window.setTimeout(function() {
            if (!isUpdated) {
                window.external.notify(JSON.stringify({Action:'update', InputValue:document.getElementById('theInput').value}));
                isUpdated = true;
            }
        }, 1000);
        if (isUpdated) {
            isUpdated = false;
        }
    }

    document.onkeypress = hideOnIdle;
})();";

        public LargeTextEditor()
        {
            InitializeComponent();
            var pageHtml = string.Format("<!doctype html><html><script type='text/javascript'>{0}</script><body><input type=text spellcheck='true' autofocus='true' id='theInput' style='padding:0px;margin:0px;border:0px;width:100%;'></body></html>",
                idleDetectJavaScript);
            this.browser.NavigateToString(pageHtml);
            this.browser.ScriptNotify += browser_ScriptNotify;
        }

        class JSEventArgs
        {
            public string Action { get; set; }
            public string InputValue { get; set; }
        }

        void browser_ScriptNotify(object sender, NotifyEventArgs e)
        {
            try
            {
                var eventArgs = JsonConvert.DeserializeObject<JSEventArgs>(e.Value);
                if (eventArgs.Action == "update")
                {
                    Text = eventArgs.InputValue;
                }
                else if (eventArgs.Action == "submit")
                {
                    Text = eventArgs.InputValue;
                    if (SubmitCommand != null)
                        SubmitCommand.Execute(null);
                }
            }catch {}
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LargeTextEditor), new PropertyMetadata(""));

        public ICommand SubmitCommand
        {
            get { return (ICommand)GetValue(SubmitCommandProperty); }
            set { SetValue(SubmitCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SubmitCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SubmitCommandProperty =
            DependencyProperty.Register("SubmitCommand", typeof(ICommand), typeof(LargeTextEditor), new PropertyMetadata(null));


    }
}
