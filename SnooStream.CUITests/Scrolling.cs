using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.VisualStudio.TestTools.UITest.Input;
using Microsoft.VisualStudio.TestTools.UITest.Extension;
using Microsoft.VisualStudio.TestTools.UITesting.WindowsRuntimeControls;
using System.Diagnostics;
using System.Collections.Generic;


namespace SnooStream.CUITests
{
    /// <summary>
    /// Summary description for CodedUITest1
    /// </summary>
    [CodedUITest]
    public class Scrolling
    {
        public Scrolling()
        {
        }

        [TestMethod]
        public void EndlessScroll()
        {

            var allChildren = GetSubredditChildren("pics");
            var childEnum = allChildren.GetEnumerator();
            childEnum.MoveNext();
            HashSet<string> itemNames = new HashSet<string>();
            for (int i = 0; i < 200; i++)
            {
                childEnum.Current.WaitForControlReady(-1);
                UIMap.UISnooStreamBetaWindow.UILinksListViewList.WaitForControlReady(-1);
                for (int ii = 0; ii < 5 && itemNames.Contains(childEnum.Current.Name); ii++)
                    Playback.Wait(5);
                
                if(itemNames.Contains(childEnum.Current.Name))
                    Debug.WriteLine("found duplicate link " + childEnum.Current.Name);
                else
                    itemNames.Add(childEnum.Current.Name);
                if (!childEnum.MoveNext())
                {
                    //end of collection, something bad happened on the test side
                    break;
                }
            }
        }

        private XamlButton FindButton(XamlControl container, string propertyName, string name)
        {
            try
            {
                var button = new XamlButton(container);
                button.SearchProperties.Add(propertyName, name);
                if (button.Exists)
                    return button;
                else
                    throw new UITestControlNotFoundException();
            }
            catch(Exception)
            {
                throw new UITestControlNotFoundException();
            }
        }

        private UITestControlCollection GetSubredditChildren(string subreddit)
        {
            var applicationWindow = XamlWindow.Launch("{D3471483-AD89-4CDA-8C7C-EF433DC8515F}:App:14678WaffleStudios.SnooStreamBeta_pprmcrjyrm180!App");
            while (!UIMap.UISnooStreamBetaWindow.UISubredditsHubSection.Exists)
            {
                Device.HardwareButton.Back();
            }

            var targetSubredditButton = FindButton(UIMap.UISnooStreamBetaWindow.UISubredditsHubSection.UIListBoxList.UISubredditGrouppopulaGroup, "Name", subreddit);
            Gesture.Tap(targetSubredditButton);
            UIMap.UISnooStreamBetaWindow.UILinksListViewList.WaitForControlExist(-1);
            var allChildren = UIMap.UISnooStreamBetaWindow.UILinksListViewList.GetChildren();
            while (allChildren.FirstOrDefault() == null)
            {
                allChildren = UIMap.UISnooStreamBetaWindow.UILinksListViewList.GetChildren();
                Playback.Wait(5);
            }
            return allChildren;
        }

        private void WaitForDataContextReady(HashSet<string> existingNames, UITestControl parent, UITestControl control)
        {
            control.WaitForControlReady(-1);
            parent.WaitForControlReady(-1);
            for (int ii = 0; ii < 5 && existingNames.Contains(control.Name); ii++)
                Playback.Wait(5);

            if (existingNames.Contains(control.Name))
                Debug.WriteLine("found duplicate link " + control.Name);
            else
                existingNames.Add(control.Name);
        }

        [TestMethod]
        public void EndlessContentScroll()
        {
            var allChildren = GetSubredditChildren("adviceanimals");
            HashSet<string> itemNames = new HashSet<string>();
            int count = 0;
            foreach (var item in allChildren)
            {
                WaitForDataContextReady(itemNames, UIMap.UISnooStreamBetaWindow.UILinksListViewList, item);
                if (count++ > 100)
                {
                    Gesture.Tap(FindButton(item as XamlControl, "AutomationId", "previewSection"));
                    UIMap.UISnooStreamBetaWindow.UIFlipViewFlipView.WaitForControlReady(-1);
                    var flipViewItems = UIMap.UISnooStreamBetaWindow.UIFlipViewFlipView.Items;
                    foreach (var child in UIMap.UISnooStreamBetaWindow.UIFlipViewFlipView.GetChildren())
                    {
                        WaitForDataContextReady(itemNames, UIMap.UISnooStreamBetaWindow.UIFlipViewFlipView, child);
                        if (count++ > 119)
                            break;

                        UIMap.UISnooStreamBetaWindow.UIFlipViewFlipView.FlipNext();
                    }
                    break;
                }
            }
        }

        public UIMap UIMap
        {
            get
            {
                if ((this.map == null))
                {
                    this.map = new UIMap();
                }

                return this.map;
            }
        }

        private UIMap map;
    }
}
