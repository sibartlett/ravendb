﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;
using Raven.Imports.Newtonsoft.Json;
using Raven.Studio.Extensions;
using Raven.Studio.Models;
using Raven.Studio.Infrastructure;

namespace Raven.Studio.Features.Query
{
    public class QueryHistoryManager
    {
        private const int MaxStoredHistoryEntries = 100;

        private readonly string databaseName;
        private readonly LinkedList<SavedQuery> recentQueries = new LinkedList<SavedQuery>();
        private readonly Dictionary<string, LinkedListNode<SavedQuery>> queriesByHash = new Dictionary<string, LinkedListNode<SavedQuery>>();
        private DateTime lastCleanupStarted = DateTime.MinValue;
        public event EventHandler<EventArgs> QueriesChanged;

        public QueryHistoryManager(string databaseName)
        {
            this.databaseName = databaseName;
            LoadPreviousQueriesFromDiskInBackground();
        }

        private void LoadPreviousQueriesFromDiskInBackground()
        {
            Task.Factory.StartNew(() =>
                                      {
                                          using (var storage = IsolatedStorageFile.GetUserStoreForSite())
                                          {
                                              var path = "QueryHistory/" + databaseName;
                                              if (!storage.DirectoryExists(path))
                                              {
                                                  storage.CreateDirectory(path);
                                              }

                                              var files = storage.GetFileNames(path + "/*.query");

                                              var queries = (from fileName in files
                                                             let content = storage.ReadEntireFile(path + "/" + fileName)
                                                             let lastWriteTime = storage.GetLastWriteTime(path + "/" + fileName)
                                                             let query =
                                                                 JsonConvert.DeserializeObject<SavedQuery>(content)
                                                             orderby lastWriteTime descending
                                                             select query)
                                                  .ToList();

                                              return queries;
                                          }
                                      })
                .ContinueOnSuccessInTheUIThread(queries =>
                                                    {
                                                        foreach (var query in queries)
                                                        {
                                                            LinkedListNode<SavedQuery> node;

                                                            if (!queriesByHash.TryGetValue(query.Hashcode, out node))
                                                            {
                                                                node = new LinkedListNode<SavedQuery>(query);
                                                                queriesByHash.Add(query.Hashcode, node);
                                                                recentQueries.AddLast(node);
                                                            }
                                                            else
                                                            {
                                                                // if the query was found, that must mean
                                                                // the user has just updated it, so don't overwrite anything
                                                            }
                                                        }

                                                        OnQueriesChanged(EventArgs.Empty);
                                                    });
        }

        public void StoreQuery(QueryState state)
        {
            var hash = state.GetHash();

            LinkedListNode<SavedQuery> node;

            if (!queriesByHash.TryGetValue(hash, out node))
            {
                node = new LinkedListNode<SavedQuery>(new SavedQuery(state.IndexName, state.Query));
                queriesByHash.Add(hash, node);
            }
            else
            {
                recentQueries.Remove(node);
            }

            node.Value.UpdateFrom(state);
            recentQueries.AddFirst(node);

            WriteToDisk(node.Value);
            OnQueriesChanged(EventArgs.Empty);
        }

        private void CleanupStoredHistory()
        {
            if (DateTime.Now - lastCleanupStarted > TimeSpan.FromMinutes(2))
            {
                lastCleanupStarted = DateTime.Now;

                Task.Factory.StartNew(
                    () =>
                        {

                            using (var storage = IsolatedStorageFile.GetUserStoreForSite())
                            {
                                var path = "QueryHistory/" + databaseName;
                                if (!storage.DirectoryExists(path))
                                {
                                    return;
                                }

                                var fileNames = storage.GetFileNames(path + "/*.query");
                                if (fileNames.Length < MaxStoredHistoryEntries)
                                {
                                    return;
                                }

                                var oldestFiles = (from file in fileNames
                                                   let fullPath = path + "/" + file
                                                   let lastWriteTime =
                                                       storage.GetLastWriteTime(fullPath)
                                                   orderby lastWriteTime descending
                                                   select fullPath)
                                    .Skip(MaxStoredHistoryEntries)
                                    .ToList();

                                foreach (var filePath in oldestFiles)
                                {
                                    storage.DeleteFile(filePath);
                                }
                            }

                        })
                    .CatchIgnore();
            }
        }

        private void WriteToDisk(SavedQuery query)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForSite())
                {
                    var path = "QueryHistory/" + databaseName;
                    if (!storage.DirectoryExists(path))
                    {
                        storage.CreateDirectory(path);
                    }

                    storage.WriteAllToFile(path + "/" + query.Hashcode + ".query", JsonConvert.SerializeObject(query, Formatting.None));
                }
            }
            catch (IsolatedStorageException ex)
            {
                ApplicationModel.Current.AddErrorNotification(ex, "Could not store query in query history");
            }

            CleanupStoredHistory();
        }

        public QueryState GetMostRecentStateForIndex(string indexName)
        {
            var savedQuery = recentQueries.FirstOrDefault(q => q.IndexName == indexName);

            if (savedQuery != null)
            {
                return ToQueryState(savedQuery);
            }
            else
            {
                return null;
            }
        }

        private static QueryState ToQueryState(SavedQuery savedQuery)
        {
            return new QueryState(savedQuery.IndexName, savedQuery.Query, savedQuery.SortOptions);
        }

        public IEnumerable<SavedQuery> RecentQueries
        {
            get { return recentQueries; }
        }

        protected void OnQueriesChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = QueriesChanged;
            if (handler != null) handler(this, e);
        }

        public QueryState GetStateByHashCode(string hashCode)
        {
            LinkedListNode<SavedQuery> node;
            return queriesByHash.TryGetValue(hashCode, out node) ? ToQueryState(node.Value) : null;
        }
    }
}
