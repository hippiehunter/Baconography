using Newtonsoft.Json;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public class UserCredential
    {
        [JsonProperty("oauth")]
		public RedditOAuth OAuth { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("me")]
        public Thing Me { get; set; }
        [JsonProperty("isdefault")]
        public bool IsDefault { get; set; }
    }

    public interface IUserCredentialService
    {
        Task<IEnumerable<UserCredential>> StoredCredentials();
        Task AddStoredCredential(UserCredential newCredential);
        Task RemoveStoredCredential(string username);
    }
}
