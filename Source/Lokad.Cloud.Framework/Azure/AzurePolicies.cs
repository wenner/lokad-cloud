﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>
	/// Azure retry policies for corner-situation and server errors.
	/// </summary>
	internal static class AzurePolicies
	{
		/// <summary>
		/// Retry policy to temporarily back off in case of transient Azure server
		/// errors, system overload or in case the denial of service detection system
		/// thinks we're a too heavy user. Blocks the thread while backing off to
		/// prevent further requests for a while (per thread).
		/// </summary>
		public static ActionPolicy TransientServerErrorBackOff { get; private set; }

		/// <summary>
		/// Very patient retry policy to deal with container or queue instantiation
		/// that happens just after a deletion.
		/// </summary>
		public static ActionPolicy SlowInstantiation { get; private set; }

		/// <summary>
		/// Static Constructor
		/// </summary>
		static AzurePolicies()
		{
			TransientServerErrorBackOff = ActionPolicy.With(TransientServerErrorExceptionFilter)
				.Retry(30, OnTransientServerErrorRetry);

			SlowInstantiation = ActionPolicy.With(SlowInstantiationExceptionFilter)
				.Retry(30, OnSlowInstantiationRetry);
		}

		static void OnTransientServerErrorRetry(Exception exception, int count)
		{
			// NOTE: we can't log here, since logging would fail as well

			// quadratic backoff, capped at 5 minutes
			var c = count + 1;
			SystemUtil.Sleep(Math.Min(300, c * c).Seconds());
		}

		static void OnSlowInstantiationRetry(Exception exception, int count)
		{
			// linear backoff
			SystemUtil.Sleep((100 * count).Milliseconds());
		}

		static bool TransientServerErrorExceptionFilter(Exception exception)
		{
			var serverException = exception as StorageServerException;
			if (serverException == null)
			{
				// we only handle server exceptions
				return false;
			}

			var errorCode = serverException.ErrorCode;
			var errorString = serverException.ExtendedErrorInformation.ErrorCode;

			if (errorCode == StorageErrorCode.ServiceInternalError
				|| errorCode == StorageErrorCode.ServiceTimeout
				|| errorString == StorageErrorCodeStrings.InternalError
				|| errorString == StorageErrorCodeStrings.ServerBusy
				|| errorString == StorageErrorCodeStrings.OperationTimedOut)
			{
				return true;
			}

			return false;
		}

		static bool SlowInstantiationExceptionFilter(Exception exception)
		{
			var clientException = exception as StorageClientException;
			if(clientException == null)
			{
				// we only handle client exceptions
				return false;
			}

			var errorCode = clientException.ErrorCode;
			var errorString = clientException.ExtendedErrorInformation.ErrorCode;

			// those 'client' exceptions reflects server-side problem (delayed instantiation)
			if (errorCode == StorageErrorCode.ResourceNotFound
				|| errorCode == StorageErrorCode.ContainerNotFound
				|| errorString == QueueErrorCodeStrings.QueueNotFound)
			{
				return true;
			}

			return false;
		}
	}
}