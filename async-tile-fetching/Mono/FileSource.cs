﻿//-----------------------------------------------------------------------
// <copyright file="FileSource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Mono {


	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Mapbox.Platform;


	/// <summary>
	///     Mono implementation of the FileSource class. It will use Mono's
	///     <see href="http://www.mono-project.com/docs/advanced/runtime/">runtime</see> to
	///     asynchronously fetch data from the network via HTTP or HTTPS requests.
	/// </summary>
	/// <remarks>
	///     This implementation requires .NET 4.5 and later. The access token is expected to
	///     be exported to the environment as MAPBOX_ACCESS_TOKEN.
	/// </remarks>
	public sealed class FileSource : IFileSource {


		private readonly List<HTTPRequest_v2> _requests = new List<HTTPRequest_v2>();
		private readonly string _accessToken = Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");


		/// <summary>Length of rate-limiting interval in seconds. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private int? XRateLimitInterval;
		/// <summary>Maximum number of requests you may make in the current interval before reaching the limit. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private long? XRateLimitLimit;
		/// <summary>Timestamp of when the current interval will end and the ratelimit counter is reset. https://www.mapbox.com/api-documentation/#rate-limits </summary>
		private DateTime? XRateLimitReset;


		/// <summary> Performs a request asynchronously. </summary>
		/// <param name="url"> The HTTP/HTTPS url. </param>
		/// <param name="callback"> Callback to be called after the request is completed. </param>
		/// <returns>
		///     Returns a <see cref="IAsyncRequest" /> that can be used for canceling a pending
		///     request. This handle can be completely ignored if there is no intention of ever
		///     canceling the request.
		/// </returns>
		public IAsyncRequest Request(string url, Action<Response> callback) {
			if (_accessToken != null) {
				url += "?access_token=" + _accessToken;
			}

			//var request = new HTTPRequest_v2(url, proxyResponse);
			var request = new HTTPRequest_v2(url, callback);
			_requests.Add(request);

			return request;
		}


		private void proxyResponse(Response response) {
			if (response.XRateLimitInterval.HasValue) { XRateLimitInterval = response.XRateLimitInterval; }
			if (response.XRateLimitLimit.HasValue) { XRateLimitLimit = response.XRateLimitLimit; }
			if (response.XRateLimitReset.HasValue) { XRateLimitReset = response.XRateLimitReset; }
			//callback(response);
		}


		/// <summary>
		///     Block until all the requests are processed.
		/// </summary>
		public void WaitForAllRequests() {
			while (true) {
				// Reverse for safely removing while iterating.
				for (int i = _requests.Count - 1; i >= 0; i--) {
					while (!_requests[i].IsCompleted) {
						Thread.Sleep(50);
					}
					_requests.RemoveAt(i);
				}

				if (_requests.Count == 0) {
					break;
				}

#if !WINDOWS_UWP
				Thread.Sleep(10);
#else
				System.Threading.Tasks.Task.Delay(5).Wait();
#endif
			}
		}
	}
}
