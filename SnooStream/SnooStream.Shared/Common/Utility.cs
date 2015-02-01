using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SnooStream.Common
{
    public static class Utility
    {
        public static SolidColorBrush GetColorFromHexa(string hexaColor)
        {
            return new SolidColorBrush(
                Color.FromArgb(
                    Convert.ToByte(hexaColor.Substring(1, 2), 16),
                    Convert.ToByte(hexaColor.Substring(3, 2), 16),
                    Convert.ToByte(hexaColor.Substring(5, 2), 16),
                    Convert.ToByte(hexaColor.Substring(7, 2), 16)
                )
            );
        }

        public const string ReadMailGlyph = "\uE166";
        public const string UnreadMailGlyph = "\uE119";



        private static bool loadingActiveLockScreen = false;

        public static string CleanRedditLink(string userInput, string username)
        {
            if (userInput == "/")
                return userInput;

            if (!string.IsNullOrWhiteSpace(username))
            {
                var selfMulti = "/" + username + "/m/";
                if (userInput.Contains(selfMulti))
                {
                    return "/me/m/" + userInput.Substring(userInput.IndexOf(selfMulti) + selfMulti.Length);
                }
            }

            if (userInput.StartsWith("me/m/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/m/"))
                return "/me" + userInput;
            else if (userInput.StartsWith("/me/m/"))
                return userInput;

            if (userInput.StartsWith("/u/"))
            {
                return userInput.Replace("/u/", "/user/");
            }

            if (userInput.StartsWith("r/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/") && !userInput.StartsWith("/r/"))
                return "/r" + userInput;
            else if (userInput.StartsWith("/r/"))
                return userInput;
            else
                return "/r/" + userInput;
        }

        public static void SafeScrollIntoView(this ListViewBase thisp, object viewModel)
        {
            if (viewModel == null)
                thisp.ScrollIntoView(null);
            else
                thisp.ScrollIntoView(viewModel, ScrollIntoViewAlignment.Leading);
        }
    }
}
