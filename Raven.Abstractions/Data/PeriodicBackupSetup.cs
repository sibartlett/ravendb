﻿//-----------------------------------------------------------------------
// <copyright file="BackupStatus.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Raven.Abstractions.Data
{
	public class PeriodicBackupSetup
	{
		public const string RavenDocumentKey = "Raven/Backup/Periodic/Setup";

		public string GlacierVaultName { get; set; }
		public string S3BucketName { get; set; }
		public string AwsRegionEndpoint { get; set; }

		public string LocalFolderName { get; set; }

		public int IntervalMilliseconds { get; set; }
	}

	public class PeriodicBackupStatus
	{
		public const string RavenDocumentKey = "Raven/Backup/Periodic/Status";
		public Etag LastDocsEtag { get; set; }
		public Etag LastAttachmentsEtag { get; set; }

		public PeriodicBackupStatus()
		{
			LastDocsEtag = Etag.Empty;
			LastAttachmentsEtag = Etag.Empty;
		}
	}
}
