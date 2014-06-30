using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class CaptchaService : ICaptchaProvider
    {
        public async Task<string> GetCaptchaResponse(string captchaIden)
        {
            var captcha = new CaptchaViewModel(captchaIden);
            var popupResult = await SnooStreamViewModel.NavigationService.ShowPopup(captcha);
            if (popupResult)
            {
                return captcha.CaptchaResponse;
            }
            else
                return null;
        }
    }
}
