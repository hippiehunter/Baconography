using KitaroDB;
using Newtonsoft.Json;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public class DefaultUserCredentialService : IUserCredentialService
    {

        public DefaultUserCredentialService()
        {
            _userInfoDbPath = SnooStreamViewModel.CWD + "\\userinfodb.ism";
        }

        private string _userInfoDbPath;
        private DB _userInfoDb;
        private DB GetUserInfoDB()
        {
            if (_userInfoDb == null)
            {
                lock (this)
                {
                    if (_userInfoDb == null)
                    {
                        _userInfoDb = DB.Create(_userInfoDbPath, DBCreateFlags.None, ushort.MaxValue - 100,
                        new DBKey[] { new DBKey(8, 0, DBKeyFlags.KeyValue, "default", true, false, false, 0) });
                    }
                }
                
            }
            return _userInfoDb;
        }

        private void AddStoredCredentialImpl(UserCredential newCredential, string password)
        {
            var userInfoDb = GetUserInfoDB();

            try
            {
                var currentCredentials = GetStoredCredentialsImpl();
                var existingCredential = currentCredentials.FirstOrDefault(credential => credential.Username == newCredential.Username);
                if (existingCredential != null)
                {
                    var lastCookie = existingCredential.LoginCookie;
                    //we already exist in the credentials, just update our login token and password (if its set)
                    if (existingCredential.LoginCookie != newCredential.LoginCookie ||
                        existingCredential.IsDefault != newCredential.IsDefault)
                    {
                        existingCredential.LoginCookie = newCredential.LoginCookie;
                        existingCredential.IsDefault = newCredential.IsDefault;

                        //go find the one we're updating and actually do it
                        var userCredentialsCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.AutoLock);
                        if (userCredentialsCursor != null)
                        {
                            using (userCredentialsCursor)
                            {
                                do
                                {
                                    var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                                    if (credential.Username == newCredential.Username)
                                    {
                                        userCredentialsCursor.Update(JsonConvert.SerializeObject(existingCredential));
                                        break;
                                    }
                                } while (userCredentialsCursor.MoveNext());
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        AddOrUpdateWindowsCredential(existingCredential, password, lastCookie);
                    }
                }
                else
                {
                    userInfoDb.Insert("credentials", JsonConvert.SerializeObject(newCredential));
                    var newPassData = new PasswordData { Password = password, LastCookie = newCredential.LoginCookie };
                    userInfoDb.Insert("passwords", JsonConvert.SerializeObject(newPassData));
                }
            }
            catch
            {
                //let it fail
            }
        }

        private void RemoveStoredCredentialImpl(string username)
        {
            try
            {
                var userInfoDb = GetUserInfoDB();
                List<string> lastCookies = new List<string>();
                //go find the one we're updating and actually do it
                var userCredentialsCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.AutoLock);
                if (userCredentialsCursor != null)
                {
                    using (userCredentialsCursor)
                    {
                        do
                        {
                            var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                            if (credential.Username == username)
                            {
                                lastCookies.Add(credential.LoginCookie);
                                userCredentialsCursor.Delete();
                            }
                        } while (userCredentialsCursor.MoveNext());
                    }
                }

                var passwordCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "passwords", DBReadFlags.AutoLock);
                if (passwordCursor != null)
                {
                    using (passwordCursor)
                    {
                        do
                        {
                            var passwordData = JsonConvert.DeserializeObject<PasswordData>(passwordCursor.GetString());
                            if (lastCookies.Contains(passwordData.LastCookie))
                            {
                                passwordCursor.Delete();
                            }
                        } while (passwordCursor.MoveNext());
                    }
                }
            }
            catch
            {
                //let it fail
            }

        }

        private void AddOrUpdateWindowsCredential(UserCredential existingCredential, string password, string lastCookie)
        {
            var userInfoDb = GetUserInfoDB();
            try
            {

                var passwordCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "passwords", DBReadFlags.AutoLock);
                if (passwordCursor != null)
                {
                    using (passwordCursor)
                    {
                        do
                        {
                            var passwordData = JsonConvert.DeserializeObject<PasswordData>(passwordCursor.GetString());
                            if (lastCookie == passwordData.LastCookie)
                            {
                                var newPassData = new PasswordData { Password = password, LastCookie = existingCredential.LoginCookie };
                                passwordCursor.Update(JsonConvert.SerializeObject(newPassData));
                                break;
                            }
                        } while (passwordCursor.MoveNext());
                    }
                }
            }
            catch
            {
                //let it fail
            }
        }

        private List<UserCredential> GetStoredCredentialsImpl()
        {
            List<UserCredential> credentials = new List<UserCredential>();
            var userInfoDb = GetUserInfoDB();
            var userCredentialsCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.NoLock);
            if (userCredentialsCursor != null)
            {
                using (userCredentialsCursor)
                {
                    do
                    {
                        var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                        credentials.Add(credential);
                    } while (userCredentialsCursor.MoveNext());
                }
            }
            return credentials;
        }

        private class PasswordData
        {
            [JsonProperty("lastcookie")]
            public string LastCookie { get; set; }
            [JsonProperty("password")]
            public string Password { get; set; }
        }

        public Task<IEnumerable<UserCredential>> StoredCredentials()
        {
            return Task.Run(() => (IEnumerable<UserCredential>)GetStoredCredentialsImpl());
        }

        public Task AddStoredCredential(UserCredential newCredential, string password)
        {
            return Task.Run(() => AddStoredCredentialImpl(newCredential, password));
        }

        public Task RemoveStoredCredential(string username)
        {
            return Task.Run(() => RemoveStoredCredentialImpl(username));
        }
    }
}
