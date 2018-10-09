using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

using EeekSoft.Asynchronous;
using System.Threading;

namespace asyncUseIterator {
	class Program {
		static void Main(string[] args) {

			Console.WriteLine("\nMain: Current Thread Id :{0}", Thread.CurrentThread.ManagedThreadId);

			// Download the URLs and wait until all of them complete
			DownloadAll().ExecuteAndWait();
			Console.ReadLine();
		}

		/// <summary>
		/// Asynchronous method that downloads the specified url and prints the HTML title
		/// </summary>
		static IEnumerable<IAsync> AsyncMethod(string url) {
			WebRequest req = HttpWebRequest.Create(url);
			Console.WriteLine("[{0}] starting. id: {1}", url, Thread.CurrentThread.ManagedThreadId);

			// asynchronously get the response from http server
			Async<WebResponse> response = req.GetResponseAsync();
			yield return response;

			Console.WriteLine("[{0}] got response. id: {1}", url, Thread.CurrentThread.ManagedThreadId);
			Stream resp = response.Result.GetResponseStream();

			// download HTML using the asynchronous extension method
			// instead of using synchronous StreamReader
			Async<string> html = resp.ReadToEndAsync().ExecuteAsync<string>();
			yield return html;

			// extract and print the HTML title
			Regex reg = new Regex(@"<title[^>]*>(.*)</title[^>]*>");
			string title = reg.Match(html.Result).Groups[1].Value;
			title = "".PadLeft((78 - title.Length) / 2) +
				title + "".PadRight((78 - title.Length) / 2);
			Console.WriteLine("[{0}] completed\n{2}\n{1}\n{2}",
				url, title, "".PadRight(79, '*'));
		}


		/// <summary>
		/// Method which performs several HTTP requests asyncrhonously in parallel
		/// </summary>
		static IEnumerable<IAsync> DownloadAll() {
			var methods = Async.Parallel(
				AsyncMethod("http://www.microsoft.com"));
				//AsyncMethod("http://www.google.com"),
				//AsyncMethod("http://www.apple.com"),
				//AsyncMethod("http://www.novell.com"));
			yield return methods;

			Console.WriteLine("Completed all! id: {0}", Thread.CurrentThread.ManagedThreadId);
		}
	}
}
