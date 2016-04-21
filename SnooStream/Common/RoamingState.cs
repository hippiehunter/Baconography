using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using SnooSharp;
using SnooStream.ViewModel.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SnooStream.Common
{
    class RoamingState
    {
        public RoamingState()
        {
            ApplicationData.Current.DataChanged += Current_DataChanged;
        }

        private void Current_DataChanged(ApplicationData sender, object args)
        {
            Messenger.Default.Send<SettingsChangedMessage>(new SettingsChangedMessage());
        }

        private JsonSerializer _serializer = new JsonSerializer();
        public T Get<T>(string key) where T : class
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            try
            {
                object value;
                if (roamingSettings.Values.TryGetValue(key, out value))
                {
                    var bsonReader = new JsonTextReader(new StringReader(value as string));
                    return _serializer.Deserialize<T>(bsonReader);
                }
            }
            catch (Exception)
            {
                //TODO report failure/corruption
                roamingSettings.Values.Remove(key);
            }
            return null;
        }

        public void Set<T>(string key, T t)
        {
            try
            {
                var roamingSettings = ApplicationData.Current.RoamingSettings;
                var targetStream = new StringWriter();
                var bsonReader = new JsonTextWriter(targetStream);
                _serializer.Serialize(bsonReader, t);
                roamingSettings.Values[key] = targetStream.GetStringBuilder().ToString();
            }
            catch (Exception)
            {
                //TODO report failure/corruption
            }
        }

        public Dictionary<string, string> Settings
        {
            get
            {
                return Get<Dictionary<string, string>>("settings");
            }
            set
            {
                Set("settings", value);
            }
        }

        public List<Dictionary<string, object>> NavStack
        {
            get
            {
                return Get<List<Dictionary<string, object>>>("navstack");
            }
            set
            {
                Set("navstack", value);
            }
        }

        public List<UserState> UserCredentials
        {
            get
            {
                return Get<List<UserState>>("credentials") ?? new List<UserState>();
            }
            set
            {
                Set("credentials", value);
            }
        }
    }
}
