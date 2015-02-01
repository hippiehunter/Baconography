using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UITesting;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.VisualStudio.TestTools.UITest.Input;
using Microsoft.VisualStudio.TestTools.UITest.Extension;
using Microsoft.VisualStudio.TestTools.UITesting.WindowsRuntimeControls;
using System.Diagnostics;


namespace SnooStream.CUITests
{
    /// <summary>
    /// Summary description for CodedUITest1
    /// </summary>
    [CodedUITest]
    public class CodedUITest1
    {
        public CodedUITest1()
        {
        }

        [TestMethod]
        public void CodedUITestMethod1()
        {
            XamlWindow.Launch("{D3471483-AD89-4CDA-8C7C-EF433DC8515F}:App:14678WaffleStudios.SnooStreamBeta_pprmcrjyrm180!App");
            var children = UIMap.UISnooStreamBetaWindow.UISubredditsHubSection.UIListBoxList.GetChildren();
            foreach (var child in children)
            {
                Debug.WriteLine(child.Name);
            }
            Gesture.Tap(UIMap.UISnooStreamBetaWindow.UISubredditsHubSection.UIListBoxList.UISubredditGrouppopulaGroup.UISubredditWrapperpopuListItem.UIPicsButton);
            //Gesture.Flick()
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
