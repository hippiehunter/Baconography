using KitaroDB;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class OfflineService : IActionDeferralSink
    {
        private string _blobsFileName;
        private string _linksFileName;
        private string _subredditStatisticsFileName;
        private string _domainStatisticsFileName;
        private string _actionDeferalFileName;

        private DB _blobsDb;
        private DB _linksDb;
        private DB _subredditStatisticsDb;
        private DB _domainStatisticsDb;
        private DB _actionDeferalDb;

        public void Clear()
        {
            Cleanup(_blobsDb, 4, 20, 2, TimeSpan.FromTicks(0));
            PurgeDB(_linksDb, _linksFileName);
            PurgeDB(_subredditStatisticsDb, _subredditStatisticsFileName);
            PurgeDB(_domainStatisticsDb, _domainStatisticsFileName);
        }

        private void PurgeDB(DB db, string filename)
        {
            db.Dispose();
            DB.Purge(filename);
        }

        public OfflineService()
        {
            _blobsFileName = SnooStreamViewModel.CWD + "\\blobs_v4.ism";
            _linksFileName = SnooStreamViewModel.CWD + "\\links_v4.ism";
            _subredditStatisticsFileName = SnooStreamViewModel.CWD + "\\subreddit_statistics_v1.ism";
            _domainStatisticsFileName = SnooStreamViewModel.CWD + "\\domain_statistics_v1.ism";
            _actionDeferalFileName = SnooStreamViewModel.CWD + "\\actionss_v1.ism";

            
            _blobsDb = DB.Create(_blobsFileName, DBCreateFlags.None, 0, new DBKey[]
            { 
                new DBKey(4, 0, DBKeyFlags.Integer, "default", false, false, false, 0),
                new DBKey(8, 4, DBKeyFlags.AutoTime, "timestamp", false, true, false, 1),
                new DBKey(8, 12, DBKeyFlags.AutoSequence, "timestamp", false, false, false, 2)
            });

            _actionDeferalDb = DB.Create(_actionDeferalFileName, DBCreateFlags.None, 0, new DBKey[]
            { 
                new DBKey(8, 0, DBKeyFlags.AutoSequence, "insertion-order", false, false, false, 0) 
            });

            _linksDb = DB.Create(_linksFileName, DBCreateFlags.None, 0, new DBKey[]
            {
                new DBKey(10, 0, DBKeyFlags.Alpha, "thing-id", false, false, false, 0),
                new DBKey(8, 10, DBKeyFlags.AutoTime, "timestamp", false, true, false, 1),
                new DBKey(8, 18, DBKeyFlags.AutoSequence, "insertion-order", false, false, true, 2) 
            });

            _subredditStatisticsDb = DB.Create(_subredditStatisticsFileName, DBCreateFlags.None, 28, new DBKey[]
            {
                new DBKey(12, 0, DBKeyFlags.Alpha, "subreddit_id", false, false, false, 0),
                new DBKey(4, 12, DBKeyFlags.Unsigned, "links", true, true, false, 1),
                new DBKey(4, 16, DBKeyFlags.Unsigned, "comments", true, true, false, 2),
                new DBKey(8, 20, DBKeyFlags.AutoTime, "update_timestamp", true, true, false, 3)
            });

            _domainStatisticsDb = DB.Create(_domainStatisticsFileName, DBCreateFlags.None, 20, new DBKey[]
            {
                new DBKey(4, 0, DBKeyFlags.Unsigned, "domain_hash", false, false, false, 0),
                new DBKey(4, 4, DBKeyFlags.Unsigned, "links", true, true, false, 1),
                new DBKey(4, 8, DBKeyFlags.Unsigned, "comments", true, true, false, 2),
                new DBKey(8, 12, DBKeyFlags.AutoTime, "update_timestamp", true, true, false, 3)
            });

            _history = LoadHistory();
            
        }


        public Task CleanupAll(TimeSpan olderThan)
        {
            return Task.Run(() => 
            {
                Cleanup(_linksDb, 18, 26, 2, olderThan);
                Cleanup(_blobsDb, 4, 20, 2, olderThan);
            });
        }

        private static void Cleanup(DB db, int timeStampIndex, int importantFlagIndex, int seqKeyId, TimeSpan olderThan)
        {
            using (var blobCursor = db.Seek(db.GetKeys()[seqKeyId], BitConverter.GetBytes((int)0), DBReadFlags.WaitOnLock | DBReadFlags.MatchGTEQ))
            {
                if (blobCursor == null)
                    return;
                do
                {
                    var gottenBlob = blobCursor.Get();

                    var important = BitConverter.ToBoolean(gottenBlob, importantFlagIndex);
                    var microseconds = BitConverter.ToInt64(gottenBlob, timeStampIndex);
                    var updatedTime = new DateTime(microseconds * 10).AddYears(1969);
                    var blobAge = DateTime.Now - updatedTime;
                    if (blobAge >= olderThan && !important)
                    {
                        blobCursor.Delete();
                    }
                        //short circut if we end up on data that is newer than the target
                    else if (blobAge < olderThan)
                        break;

                } while (blobCursor.MoveNext());

            }
        }

        public Task StoreComments(Listing listing)
        {
            return Task.Run(() =>
                {
                    try
                    {
                        if (listing == null || listing.Data.Children.Count == 0)
                            return;

                        var linkThing = listing.Data.Children.First().Data as Link;
                        if (linkThing != null)
                        {
                            StoreLinkImpl(listing.Data.Children.First());
                        }

                        StoreCommentsImpl(listing);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("exception while storing comments {0}", ex);
                    }
                });
        }

        private void StoreCommentsImpl(Listing listing)
        {
            var linkThing = listing.Data.Children.First();
            if (!(linkThing.Data is Link))
                return;


            var permalink = ((Link)linkThing.Data).Permalink;
            if (permalink.EndsWith(".json?sort=hot"))
                permalink = permalink.Replace(".json?sort=hot", "");

            
            //we can cut down on IO by about 50% by stripping out the HTML bodies of comments since we dont have any need for them
            StripCommentData(listing.Data.Children);
            StoreBlobImpl("comments:" + permalink, listing, true);
            StoreLinkMetadataImpl(new TypedThing<Link>(linkThing), ((Link)linkThing.Data).CommentCount, listing.Data.Children.Count, -1);
        }

        private void StoreLinkMetadataImpl(string linkId, Func<LinkMeta, LinkMeta> producer)
        {
            var keyBytes = Encoding.UTF8.GetBytes(linkId);
            using (var linkCursor = _linksDb.Seek(_linksDb.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
            {
                if (linkCursor != null)
                {
                    var existingData = linkCursor.Get();
                    var linkMeta = producer(JsonConvert.DeserializeObject<LinkMeta>(Encoding.UTF8.GetString(existingData, 13, existingData.Length - 13)));
                    var serializedLinkMeta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(linkMeta));
                    var recordBytes = new byte[13 + serializedLinkMeta.Length];
                    serializedLinkMeta.CopyTo(recordBytes, 13);
                    linkCursor.Update(recordBytes);
                }
                else
                {
                    var linkMeta = producer(null);
                    if (linkMeta == null)
                    {
                        Debug.WriteLine("error while storing link metadata, null result from producer");
                    }
                    else
                    {
                        var serializedLinkMeta = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(linkMeta));
                        var recordBytes = new byte[13 + serializedLinkMeta.Length];
                        serializedLinkMeta.CopyTo(recordBytes, 13);
                        _linksDb.Insert(recordBytes);
                    }
                }
            }
        }

        private void StoreLinkMetadataImpl(Thing link, int linkComments, int storedComments, int viewed = -1)
        {
            StoreLinkMetadataImpl(((Link)link.Data).Id, (existingLink) =>
                {
                    if (existingLink == null)
                        existingLink = new LinkMeta();

                    existingLink.Data = link.Data;
                    existingLink.CommentCount = linkComments;
                    existingLink.ViewedCommentCount = Math.Max(viewed, existingLink.ViewedCommentCount);
                    existingLink.OfflinedCommentCount = Math.Max(storedComments, existingLink.OfflinedCommentCount);
                    return existingLink;
                });
        }

        public Task StoreLinkMetadataViewed(string linkId, int linkComments, int viewed = -1)
        {
            return Task.Run(() =>
            {
                StoreLinkMetadataImpl(linkId, (existingLink) =>
                {
                    if (existingLink == null)
                        return null;

                    existingLink.CommentCount = linkComments;
                    existingLink.ViewedCommentCount = Math.Max(viewed, existingLink.ViewedCommentCount);
                    return existingLink;
                });
            });
        }

        private void StoreLinkImpl(Thing link)
        {
            try
            {
                int linkKeySpaceSize = 36;
                int primaryKeySpaceSize = 20;

                var value = JsonConvert.SerializeObject(link);
                var encodedValue = Encoding.UTF8.GetBytes(value);

                var combinedSpace = new byte[encodedValue.Length + linkKeySpaceSize];
                var keySpace = new byte[primaryKeySpaceSize];

                //these ids are stored in base 36 so we will never see unicode chars
                for (int i = 0; i < 8 && i < ((Link)link.Data).SubredditId.Length; i++)
                    keySpace[i] = combinedSpace[i] = (byte)((Link)link.Data).SubredditId[i];

                for (int i = 8; i < 16 && i < (byte)((Link)link.Data).Name.Length + 8; i++)
                    keySpace[i] = combinedSpace[i] = (byte)((Link)link.Data).Name[i - 8];

                var hashBytes = BitConverter.GetBytes(((Link)link.Data).Permalink.GetHashCode());
                hashBytes.CopyTo(combinedSpace, 16);
                hashBytes.CopyTo(keySpace, 16);
                encodedValue.CopyTo(combinedSpace, linkKeySpaceSize);

                using (var commentsCursor = _linksDb.Seek(_linksDb.GetKeys().First(), keySpace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock))
                {
                    if (commentsCursor != null)
                        commentsCursor.Update(combinedSpace);

                    else
                        _linksDb.Insert(combinedSpace);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("exception while storing link {0}", ex);
            }
        }

        private void StripCommentData(List<Thing> things)
        {
            if (things == null)
                return;

            foreach (var thing in things)
            {
                if (thing.Data is Comment)
                {
                    ((Comment)thing.Data).BodyHtml = "";
                    if (((Comment)thing.Data).Replies != null)
                        StripCommentData(((Comment)thing.Data).Replies.Data.Children);
                }
            }
        }

        private LinkMeta GetLinkMetadataImpl(string linkId)
        {
            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(linkId);

                using (var linkCursor = _linksDb.Seek(_linksDb.GetKeys()[0], keyBytes, DBReadFlags.NoLock))
                {
                    if (linkCursor != null)
                    {
                        var bytes = linkCursor.Get();
                        return JsonConvert.DeserializeObject<LinkMeta>(Encoding.UTF8.GetString(bytes, 13, bytes.Length - 13));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("exception while getting link metadata {0}", ex);
            }
            return null;
        }

        public Task<IEnumerable<LinkMeta>> GetLinkMetadata(IEnumerable<string> linkIds)
        {
            return Task.Run(() =>
                {
                    return (IEnumerable<LinkMeta>)linkIds.Select(GetLinkMetadataImpl).ToList();
                });
        }

        public Task<LinkMeta> GetLinkMetadata(string linkId)
        {
            return Task.Run(() => GetLinkMetadataImpl(linkId));
        }

        public Task<Listing> GetTopLevelComments(string permalink, int count)
        {
            if (permalink.EndsWith(".json?sort=hot"))
                permalink = permalink.Replace(".json?sort=hot", "");

            return RetriveBlob<Listing>("comments:" + permalink, TimeSpan.FromDays(4096));
        }

        public Task IncrementDomainStatistic(string domain, bool isLink)
        {
            return Task.Run(() => UsageStatisticsUtility.IncrementDomain(_domainStatisticsDb, domain, isLink));
        }

        public Task IncrementSubredditStatistic(string subredditId, bool isLink)
        {
            return Task.Run(() => UsageStatisticsUtility.IncrementSubreddit(_domainStatisticsDb, subredditId, isLink));
        }

        public Task<List<DomainAggregate>> GetDomainAggregates(int maxListSize, int threshold)
        {
            return Task.Run(() => UsageStatisticsUtility.GetDomainAggregateList(_domainStatisticsDb, maxListSize, threshold));
        }

        public Task<List<SubredditAggregate>> GetSubredditAggregates(int maxListSize, int threshold)
        {
            return Task.Run(() => UsageStatisticsUtility.GetSubredditAggregateList(_subredditStatisticsDb, maxListSize, threshold));
        }

        public Task StoreLinks(Listing listing)
        {
            return Task.Run(() =>
                {
                    if (listing != null && listing.Data.Children != null)
                    {
                        foreach (var link in listing.Data.Children)
                        {
                            if (link.Data is Link)
                            {
                                StoreLinkImpl(link);
                            }
                        }
                    }
                });
        }

        public Task<Thing> GetSubreddit(string name)
        {
            return Task.Run(() => RetriveBlobImpl<Thing>(name, TimeSpan.FromDays(4096), false));
        }

        private Listing AllLinksImpl(string after)
        {
            int afterInt;
            if (!int.TryParse(after, out afterInt))
                afterInt = int.MaxValue;

            List<Thing> things = new List<Thing>();
            int lastId = 0;
            using (var linkCursor = _linksDb.Seek(_linksDb.GetKeys()[2], BitConverter.GetBytes(afterInt), DBReadFlags.NoLock | DBReadFlags.MatchGTEQ))
            {
                do
                {
                    if (linkCursor == null || things.Count > 20)
                        break;

                    var linkBytes = linkCursor.Get();
                    var linkString = Encoding.UTF8.GetString(linkBytes, 27, linkBytes.Length - 27);
                    var linkThing = JsonConvert.DeserializeObject<LinkMeta>(linkString);
                    things.Add(linkThing);
                } while (linkCursor.MoveNext());
            }
            return new Listing { Data = new ListingData { Children = things, Before = after, After = lastId == 0 ? null : lastId.ToString() } };
        }

        public Task<Listing> AllLinks(string after)
        {
            return Task.Run(() => AllLinksImpl(after));
        }

        public Task StoreOrderedThings(string key, IEnumerable<Thing> things)
        {
            return StoreBlob(key, things.ToArray());
        }

        public async Task<IEnumerable<Thing>> RetrieveOrderedThings(string key, TimeSpan maxAge)
        {
            var blob = await RetriveBlob<Thing[]>(key, maxAge);
            return blob;
        }

        public void StoreInitializationBlob(InitializationBlob initBlob)
        {
            StoreBlobImpl("initBlob", initBlob, false);
        }

        public InitializationBlob LoadInitializationBlob(string userName)
        {
            return RetriveBlobImpl<InitializationBlob>("initBlob", TimeSpan.FromDays(4096), false) ?? 
                new InitializationBlob 
                { 
                    Settings = new Dictionary<string, string>(), 
                    NSFWFilter = new Dictionary<string,bool>()
                };
        }

        Dictionary<string, DateTime> _history;

        private Dictionary<string, DateTime> LoadHistory()
        {
            //this needs to remove history elements that are too old
            return RetriveBlobImpl<Dictionary<string, DateTime>>("history", TimeSpan.FromDays(4096), false) ?? new Dictionary<string, DateTime>();
        }

        public void StoreHistory()
        {
            StoreBlobImpl("history", _history, true);
        }

        public void AddHistory(string link)
        {
            var lowerLink = link.ToLower();
            if (!_history.ContainsKey(lowerLink))
                _history.Add(lowerLink, DateTime.Now);
        }

        public void ClearHistory()
        {
            _history.Clear();
            StoreHistory();
        }

        public bool HasHistory(string link)
        {
            return _history.ContainsKey(link.ToLower());
        }

        public Task StoreSubreddit(TypedThing<Subreddit> subreddit)
        {
            return Task.Run(() => StoreBlobImpl("subreddit:" + subreddit.Data.Name, subreddit, false));
        }

        public uint GetHash(string name)
        {
            return (uint)name.GetHashCode();
        }

        private byte[] CompressString(string data, byte[] lead)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            using (var compressedData = new MemoryStream())
            {
                compressedData.Write(lead, 0, lead.Length);
                using (var gzipStream = new System.IO.Compression.GZipStream(compressedData, System.IO.Compression.CompressionLevel.Fastest))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                return compressedData.ToArray();
            }
        }

        private void StoreBlobImpl(string name, object serializable, bool compression = true)
        {
            try
            {
                var leadBytes = new Byte[20];
                var keyBytes = BitConverter.GetBytes(name.GetHashCode());
                keyBytes.CopyTo(leadBytes, 0);
                var serialized = JsonConvert.SerializeObject(serializable);
                byte[] storeBytes = null;
                if (compression)
                {
                    storeBytes = CompressString(serialized, leadBytes);
                }
                else
                {
                    var serializedBytes = Encoding.UTF8.GetBytes(serialized);
                    storeBytes = new byte[20 + serializedBytes.Length];
                    leadBytes.CopyTo(storeBytes, 0);
                    serializedBytes.CopyTo(storeBytes, 20);
                }

                using (var blobCursor = _blobsDb.Seek(_blobsDb.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
                {
                    if (blobCursor != null)
                    {
                        blobCursor.Update(storeBytes);
                    }
                    else
                    {
                        _blobsDb.Insert(storeBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("while storing blob received {0}", ex.ToString());
            }       
        }

        public Task StoreBlob(string name, object serializable)
        {
            return Task.Run(() => StoreBlobImpl(name, serializable, true));
        }

        private T RetriveBlobImpl<T>(string name, TimeSpan maxAge, bool useCompression = true)
        {
            try
            {
                using (var blobCursor = _blobsDb.Seek(_blobsDb.GetKeys()[0], BitConverter.GetBytes(name.GetHashCode()), DBReadFlags.NoLock))
                {
                    if (blobCursor != null)
                    {
                        var gottenBlob = blobCursor.Get();
                        var microseconds = BitConverter.ToInt64(gottenBlob, 4);
                        var updatedTime = new DateTime(microseconds * 10).AddYears(1969);
                        var blobAge = DateTime.Now - updatedTime;
                        if (blobAge <= maxAge)
                        {
                            if (useCompression)
                            {
                                using (var memoryStream = new MemoryStream(gottenBlob))
                                {
                                    memoryStream.Seek(20, SeekOrigin.Begin);

                                    using (var uncompressed = new MemoryStream())
                                    {
                                        using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Decompress))
                                        {
                                            gzipStream.CopyTo(uncompressed);
                                        }
                                        var uncompressedBytes = uncompressed.ToArray();
                                        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(uncompressedBytes, 0, uncompressedBytes.Length));
                                    }
                                }
                            }
                            else
                            {
                                var blobString = Encoding.UTF8.GetString(gottenBlob, 20, gottenBlob.Length - 20);
                                return JsonConvert.DeserializeObject<T>(blobString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("while loading blob recived {0}", ex.ToString());
            }

            return default(T);
        }

        public Task<T> RetriveBlob<T>(string name, TimeSpan maxAge)
        {
            return Task.Run(() => RetriveBlobImpl<T>(name, maxAge, true));
        }

        public void Defer(Dictionary<string, string> arguments, string action)
        {
            var serializedDeferal = JsonConvert.SerializeObject(Tuple.Create(arguments, action));
            var deferalBytes = Encoding.UTF8.GetBytes(serializedDeferal);
            var recordBytes = new byte[deferalBytes.Length + 8];
            deferalBytes.CopyTo(recordBytes, 8);
            _actionDeferalDb.Insert(recordBytes);
        }

        public Tuple<Dictionary<string, string>, string> DequeDeferral()
        {
            try
            {
                using (var deferalCursor = _actionDeferalDb.Seek(DBReadFlags.WaitOnLock))
                {
                    if (deferalCursor != null)
                    {
                        var deferalBytes = deferalCursor.Get();
                        var deferalString = Encoding.UTF8.GetString(deferalBytes, 8, deferalBytes.Length - 8);
                        var deferal = JsonConvert.DeserializeObject<Tuple<Dictionary<string, string>, string>>(deferalString);
                        return deferal;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("recived exception while deque'ing deferal {0}", ex);
            }
            return null;
        }
    }

    internal static class UsageStatisticsUtility
    {
        static readonly int SubIdKeySpaceSize = 12;
        static readonly int DomainHashKeySpaceSize = 4;

        static readonly int SubredditKeySpaceSize = 28;
        static readonly int DomainKeySpaceSize = 20;

        public static byte[] GenerateSubIdKeyspace(string id)
        {
            var keyspace = new byte[SubIdKeySpaceSize];

            for (int i = 0; i < SubIdKeySpaceSize && i < id.Length; i++)
                keyspace[i] = (byte)id[i];

            return keyspace;
        }

        public static byte[] GenerateCombinedSubredditKeyspace(string id, uint links, uint comments)
        {
            var keyspace = new byte[SubredditKeySpaceSize];

            for (int i = 0; i < SubIdKeySpaceSize && i < id.Length; i++)
                keyspace[i] = (byte)id[i];

            BitConverter.GetBytes(links).CopyTo(keyspace, SubIdKeySpaceSize);
            BitConverter.GetBytes(comments).CopyTo(keyspace, SubIdKeySpaceSize + 4);

            return keyspace;
        }

        public static byte[] GenerateDomainHashKeyspace(uint hash)
        {
            return BitConverter.GetBytes(hash);
        }

        public static byte[] GenerateCombinedDomainKeyspace(uint hash, uint links, uint comments)
        {
            var keyspace = new byte[DomainKeySpaceSize];
            BitConverter.GetBytes(hash).CopyTo(keyspace, 0);
            BitConverter.GetBytes(links).CopyTo(keyspace, DomainHashKeySpaceSize);
            BitConverter.GetBytes(comments).CopyTo(keyspace, DomainHashKeySpaceSize + 4);

            return keyspace;
        }

        public static List<SubredditAggregate> GetSubredditAggregateList(DB subredditStatisticsDb, int maxSize, int threshold)
        {
            try
            {
                var retval = new List<SubredditAggregate>();
                using (var cursor = subredditStatisticsDb.Seek(DBReadFlags.NoLock))
                {
                    while (cursor != null)
                    {
                        var agg = new SubredditAggregate();
                        var currentRecord = cursor.Get();
                        agg.SubredditId = BitConverter.ToString(currentRecord.Take(12).ToArray(), 0);
                        agg.LinkClicks = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize).Take(4).ToArray(), 0);
                        agg.CommentClicks = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize + 4).Take(4).ToArray(), 0);
                        agg.LastModified = new DateTime(BitConverter.ToInt64(currentRecord.Skip(20).Take(8).ToArray(), 0));
                        if (agg.LinkClicks > threshold || agg.CommentClicks > threshold)
                            retval.Add(agg);
                        if (!cursor.MoveNext())
                            break;
                    }
                }

                retval = retval.Where(p => p.CommentClicks > threshold || p.LinkClicks > threshold)
                    .OrderBy(p => p.CommentClicks > p.LinkClicks ? p.CommentClicks : p.LinkClicks)
                    .Take(maxSize)
                    .ToList();
                return retval;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("encountered exception while getting subreddit aggregate list {0}", ex);
            }
            return new List<SubredditAggregate>();
        }

        public static List<DomainAggregate> GetDomainAggregateList(DB domainStatisticsDb, int maxSize, int threshold)
        {
            try
            {
                var retval = new List<DomainAggregate>();
                using (var cursor = domainStatisticsDb.Seek(DBReadFlags.NoLock))
                {
                    while (cursor != null)
                    {
                        var agg = new DomainAggregate();
                        var currentRecord = cursor.Get();
                        agg.DomainHash = BitConverter.ToUInt32(currentRecord.Take(4).ToArray(), 0);
                        agg.LinkClicks = BitConverter.ToUInt32(currentRecord.Skip(4).Take(4).ToArray(), 0);
                        agg.CommentClicks = BitConverter.ToUInt32(currentRecord.Skip(8).Take(4).ToArray(), 0);
                        agg.LastModified = new DateTime(BitConverter.ToInt64(currentRecord.Skip(12).Take(8).ToArray(), 0));
                        if (agg.LinkClicks > threshold || agg.CommentClicks > threshold)
                            retval.Add(agg);
                        if (!cursor.MoveNext())
                            break;
                    }
                }

                retval = retval.Where(p => p.CommentClicks > threshold || p.LinkClicks > threshold)
                    .OrderBy(p => p.CommentClicks > p.LinkClicks ? p.CommentClicks : p.LinkClicks)
                    .Take(maxSize)
                    .ToList();
                return retval;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("encountered exception while getting domain aggregate list {0}", ex);
            }
            return new List<DomainAggregate>();
        }

        public static void IncrementDomain(DB domainStatisticsDb, string domain, bool link)
        {
            try
            {
                uint links = 0;
                uint comments = 0;
                uint hash = (uint)domain.GetHashCode();
                var keyspace = GenerateDomainHashKeyspace(hash);

                using (var dbCursor = domainStatisticsDb.Seek(domainStatisticsDb.GetKeys()[0], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock))
                {
                    if (dbCursor != null)
                    {
                        // Decode cursor
                        var record = dbCursor.Get();
                        links = BitConverter.ToUInt32(record.Skip(DomainHashKeySpaceSize).Take(4).ToArray(), 0);
                        comments = BitConverter.ToUInt32(record.Skip(DomainHashKeySpaceSize + 4).Take(4).ToArray(), 0);
                        // Increment variable
                        if (link)
                            links++;
                        else
                            comments++;
                        // Update record
                        var combinedSpace = GenerateCombinedDomainKeyspace(hash, links, comments);
                        dbCursor.Update(combinedSpace);
                    }
                    else
                    {
                        links = (uint)(link ? 1 : 0);
                        comments = (uint)(link ? 0 : 1);


                        if (link)
                            links++;
                        else
                            comments++;
                        // Insert a fresh, zero'd record
                        var combinedSpace = GenerateCombinedDomainKeyspace(hash, links, comments);
                        domainStatisticsDb.Insert(combinedSpace);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("encountered error while incrementing domain statistics {0}", ex);
            }
        }

        public static void IncrementSubreddit(DB subredditStatisticsDb, string id, bool link)
        {
            try
            {
                uint links = 0;
                uint comments = 0;
                var keyspace = GenerateSubIdKeyspace(id);

                using (var dbCursor = subredditStatisticsDb.Seek(subredditStatisticsDb.GetKeys()[0], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock))
                {
                    if (dbCursor != null)
                    {

                        // Decode cursor
                        var record = dbCursor.Get();
                        links = BitConverter.ToUInt32(record.Skip(SubIdKeySpaceSize).Take(4).ToArray(), 0);
                        comments = BitConverter.ToUInt32(record.Skip(SubIdKeySpaceSize + 4).Take(4).ToArray(), 0);
                        // Increment variable
                        if (link)
                            links++;
                        else
                            comments++;
                        // Update record
                        var combinedSpace = GenerateCombinedSubredditKeyspace(id, links, comments);
                        dbCursor.Update(combinedSpace);
                    }
                    else
                    {
                        links = (uint)(link ? 1 : 0);
                        comments = (uint)(link ? 0 : 1);

                        if (link)
                            links++;
                        else
                            comments++;
                        // Insert a fresh, zero'd record
                        var combinedSpace = GenerateCombinedSubredditKeyspace(id, links, comments);
                        subredditStatisticsDb.Insert(combinedSpace);

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("encountered error while incrementing subreddit statistics {0}", ex);
            }
        }
    }

    public class LinkMeta : Thing
    {
        public int CommentCount { get; set; }
        public int ViewedCommentCount { get; set; }
        public int OfflinedCommentCount { get; set; }
        public bool HasPreviewImage { get; set; }
        public bool HasOfflineImage { get; set; }
        public bool HasOfflineWeb { get; set; }
        public bool HasOfflineVideo { get; set; }
    }

    public class UsageStatisticsAggregate
    {
        public uint LinkClicks { get; set; }
        public uint CommentClicks { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DomainAggregate : UsageStatisticsAggregate
    {
        public string Domain { get; set; }
        public uint DomainHash { get; set; }
    }

    public class SubredditAggregate : UsageStatisticsAggregate
    {
        public string SubredditId { get; set; }
    }
}
