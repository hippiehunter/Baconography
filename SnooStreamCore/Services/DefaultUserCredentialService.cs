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

        private void AddStoredCredentialImpl(UserCredential newCredential)
        {
            var userInfoDb = GetUserInfoDB();

            try
            {
                var currentCredentials = GetStoredCredentialsImpl();
                var existingCredential = currentCredentials.FirstOrDefault(credential => credential.Username == newCredential.Username);
                if (existingCredential != null)
                {
                    UpdateWindowsCredential(existingCredential);
				}
                else
                {
                    userInfoDb.Insert("credentials", JsonConvert.SerializeObject(newCredential));
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
                                userCredentialsCursor.Delete();
                            }
                        } while (userCredentialsCursor.MoveNext());
                    }
                }
            }
            catch
            {
                //let it fail
            }

        }

        private void UpdateWindowsCredential(UserCredential existingCredential)
        {
            var userInfoDb = GetUserInfoDB();
            try
            {

				var userCredentialsCursor = userInfoDb.Select(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.AutoLock);
				if (userCredentialsCursor != null)
				{
					using (userCredentialsCursor)
					{
						do
						{
							var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
							if (credential.Username == existingCredential.Username)
							{
								userCredentialsCursor.Update(JsonConvert.SerializeObject(existingCredential));
							}
						} while (userCredentialsCursor.MoveNext());
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

        public Task<IEnumerable<UserCredential>> StoredCredentials()
        {
            return Task.Run(() => (IEnumerable<UserCredential>)GetStoredCredentialsImpl());
        }

        public Task AddStoredCredential(UserCredential newCredential)
        {
            return Task.Run(() => AddStoredCredentialImpl(newCredential));
        }

        public Task RemoveStoredCredential(string username)
        {
            return Task.Run(() => RemoveStoredCredentialImpl(username));
        }
    }
}
