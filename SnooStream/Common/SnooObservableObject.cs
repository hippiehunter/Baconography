using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Windows.UI.Core;

namespace SnooStream.Common
{
    public class SnooObservableObject : ObservableObject
    {
        public static CoreDispatcher UIDispatcher { get; set; }
        public override async void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                //when bound to a template that has been recycled, this will cause an exception
                //and needs to just be ignored
                if (UIDispatcher.HasThreadAccess)
                    base.RaisePropertyChanged(propertyName);
                else
                    await UIDispatcher.RunIdleAsync((args) =>
                    {
                        try
                        {
                            base.RaisePropertyChanged(propertyName);
                        }
                        catch(Exception ex) { Debug.WriteLine(ex); };
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
