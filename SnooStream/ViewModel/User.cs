using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface IUserContext
    {
    }

    public class UserViewModel
    {
        private IUserContext userContext;

        public UserViewModel(IUserContext userContext)
        {
            this.userContext = userContext;
        }
    }

    class UserContext : IUserContext
    {
        public UserContext(string username)
        {

        }
    }
}
