//-----------------------------------------------------------------------
// <copyright file="IRavenQueryInspector.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using Raven.Abstractions.Data;
using Raven.Client.Document;
using Raven.Client.Connection.Async;

namespace Raven.Client.Connection
{
	/// <summary>
	/// Provide access to the underlying <see cref="IDocumentQuery{T}"/>
	/// </summary>
	internal interface IRavenQueryInspector
	{
		/// <summary>
		/// Get the name of the index being queried
		/// </summary>
		string IndexQueried { get; }

		/// <summary>
		/// Get the name of the index being queried in async queries
		/// </summary>
		string AsyncIndexQueried { get; }

#if !SILVERLIGHT
		/// <summary>
		/// Grant access to the database commands
		/// </summary>
		IDatabaseCommands DatabaseCommands { get; }
#endif

		/// <summary>
		/// Grant access to the async database commands
		/// </summary>
		IAsyncDatabaseCommands AsyncDatabaseCommands { get; }

		/// <summary>
		/// The query session
		/// </summary>
		InMemoryDocumentSessionOperations Session { get; }

		/// <summary>
		/// The last term that we asked the query to use equals on
		/// </summary>
		KeyValuePair<string, string> GetLastEqualityTerm(bool isAsync = false);

		/// <summary>
		/// Get the index query for this query
		/// </summary>
		IndexQuery GetIndexQuery(bool isAsync);
	}
}