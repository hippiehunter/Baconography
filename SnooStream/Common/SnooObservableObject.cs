using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace SnooStream.Common
{
    public class SnooObservableObject : ObservableObject
    {
        protected override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                //when bound to a template that has been recycled, this will cause an exception
                //and needs to just be ignored
                base.RaisePropertyChanged(propertyName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
