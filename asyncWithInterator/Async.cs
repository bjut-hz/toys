﻿using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;

namespace EeekSoft.Asynchronous {
	/// <summary>
	/// Represents a type with no value - alternative to C# void in 
	/// situations where void can't be used
	/// </summary>
	public class Unit {
		private Unit() { }
		static Unit() {
			Value = new Unit();
		}
		public static Unit Value { get; private set; }
	}

	/// <summary>
	/// Use this cass to return a value from asynchronous method.
	/// </summary>
	/// <example><code>
	/// // Returns hello world
	/// IEnumerable&lt;IAsync&gt; Hello() {
	///   yield return new Result&lt;String&gt;("Hello world");
	/// }
	/// </code></example>
	/// <typeparam name="T"></typeparam>
	public class Result<T> : IAsync {
		public T ReturnValue { get; private set; }
		public Result(T value) {
			ReturnValue = value;
		}

		public void ExecuteStep(Action cont) {
			throw new InvalidOperationException
				("Cannot call ExecuteStep on IAsync created as a 'Result'.");
		}
	}

	/// <summary>
	/// Provides several extension methods to standard System classes
	/// and for executing the asynchronous methods implemented using the library
	/// </summary>
	public static class AsyncExtensions {
		#region System Extensions

		/// <summary>
		/// Asynchronously gets response from the internet using BeginGetResponse method.
		/// </summary>
		public static Async<WebResponse> GetResponseAsync(this WebRequest req) {
			return new AsyncPrimitive<WebResponse>(req.BeginGetResponse, req.EndGetResponse);
		}

		/// <summary>
		/// Asynchronously reads data from a stream using BeginRead.
		/// </summary>
		/// <param name="stream">The stream on which the method is called</param>
		/// <param name="buffer">The buffer to read the data into</param>
		/// <param name="offset">Byte offset in the buffer</param>
		/// <param name="count">Maximum number of bytes to read</param>
		/// <returns>Returns non-zero if there are still some data to read</returns>
		public static Async<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count) {
			return new AsyncPrimitive<int>(
				(callback, st) => stream.BeginRead(buffer, offset, count, callback, st),
				stream.EndRead);
		}

		/// <summary>
		/// Reads asynchronously the entire content of the stream and returns it 
		/// as a string using StreamReader.
		/// </summary>
		/// <returns>Returns string using the 'Result' class.</returns>
		public static IEnumerable<IAsync> ReadToEndAsync(this Stream stream) {
			MemoryStream ms = new MemoryStream();
			int read = -1;
			while(read != 0) {
				byte[] buffer = new byte[1024];
				Async<int> count = stream.ReadAsync(buffer, 0, 1024);
				yield return count;

				Console.WriteLine("[{0}] got data: {1}", "url", count.Result);
				ms.Write(buffer, 0, count.Result);
				read = count.Result;
			}

			ms.Seek(0, SeekOrigin.Begin);
			string s = new StreamReader(ms).ReadToEnd();

			yield return new Result<string>(s);
		}

		#endregion

		#region Async Extensions

		/// <summary>
		/// Executes asynchronous method and blocks the calling thread until the operation completes.
		/// </summary>
		/// <param name="async"></param>
		public static void ExecuteAndWait(this IEnumerable<IAsync> async) {
			// 信号量初始化false，当调用set后，会唤醒阻塞的线程
			ManualResetEvent wh = new ManualResetEvent(false);
			AsyncExtensions.Run(async.GetEnumerator(),
				() => {
					Console.WriteLine("SET: Current Thread Id :{0}", Thread.CurrentThread.ManagedThreadId);
					wh.Set();
				}
				);
			Console.WriteLine("ExecuteAndWait: Current Thread Id :{0}", Thread.CurrentThread.ManagedThreadId);

			// 主线程阻塞，等待set唤醒
			wh.WaitOne();

			Console.WriteLine("ExecuteAndWaitEND: Current Thread Id :{0}", Thread.CurrentThread.ManagedThreadId);

		}


		/// <summary>
		/// Spawns the asynchronous method without waiting for the result.
		/// </summary>
		/// <param name="async"></param>
		public static void Execute(this IEnumerable<IAsync> async) {
			AsyncExtensions.Run(async.GetEnumerator());
		}

		/// <summary>
		/// Executes the asynchronous method in another asynchronous method, 
		/// and assumes that the method returns result of type T.
		/// </summary>
		public static Async<T> ExecuteAsync<T>(this IEnumerable<IAsync> async) {
			return new AsyncWithResult<T>(async);
		}

		/// <summary>
		/// Executes the asynchronous method in another asynchronous method, 
		/// and assumes that the method doesn't return any result.
		/// </summary>
		public static Async<Unit> ExecuteAsync(this IEnumerable<IAsync> async) {
			return new AsyncWithUnitResult(async);
		}

		#endregion

		#region Implementation

		internal static void Run<T>(IEnumerator<IAsync> en, Action<T> cont) {
			if(!en.MoveNext())
				throw new InvalidOperationException("Asynchronous workflow executed using"
					+ "'AsyncWithResult' didn't return result using 'Result'!");

			var res = (en.Current as Result<T>);
			if(res != null) { cont(res.ReturnValue); return; }

			en.Current.ExecuteStep
				(() => AsyncExtensions.Run<T>(en, cont));
		}

		internal static void Run(IEnumerator<IAsync> en, Action cont) {
			if(!en.MoveNext()) { cont(); return; }
			en.Current.ExecuteStep
				(() => AsyncExtensions.Run(en, cont));
		}
		// 由异步操作的回调，最终调用MoveNext，因为en只会同时由一个线程调用，要么是唤起异步操作的线程，要不是回调函数调用，因此，en不存在线程安全问题.
		// 本质异步操作的后序操作(continuation)还是是放在该异步操作的回调函数中被执行的。。但是通过使用iterator做到了同步的写法，避免了callback hell
		internal static void Run(IEnumerator<IAsync> en) {
			if(!en.MoveNext())
				return;
			en.Current.ExecuteStep
				(() => AsyncExtensions.Run(en));
		}

		#endregion
	}


	/// <summary>
	/// Provides several helper methods for working with asynchronous computations.
	/// </summary>
	public static class Async {
		/// <summary>
		/// Combines the given asynchronous methods and returns an asynchronous method that,
		/// when executed - executes the methods in parallel.
		/// </summary>
		public static Async<Unit> Parallel(params IEnumerable<IAsync>[] operations) {
			return new AsyncPrimitive<Unit>((cont) => {
				bool[] completed = new bool[operations.Length];
				for(int i = 0; i < operations.Length; i++)
					ExecuteAndSet(operations[i], completed, i, cont).Execute();
			});
		}

		#region Implementation

		private static IEnumerable<IAsync> ExecuteAndSet(IEnumerable<IAsync> op, bool[] flags, int index, Action<Unit> cont) {
			foreach(IAsync async in op)
				yield return async;
			bool allSet = true;
			lock(flags) {
				flags[index] = true;
				foreach(bool b in flags)
					if(!b) { allSet = false; break; }
			}
			if(allSet)
				cont(Unit.Value);
		}

		#endregion
	}

	/// <summary>
	/// Represents a primitive untyped asynchronous operation.
	/// This interface should be used only in asynchronous method declaration.
	/// </summary>
	public interface IAsync {
		void ExecuteStep(Action cont);
	}

	/// <summary>
	/// Represents an asynchronous computation that yields a result of type T.
	/// </summary>
	public abstract class Async<T> : IAsync {
		protected T result;
		protected bool completed = false;

		public T Result {
			get {
				if(!completed)
					throw new Exception("Operation not completed, did you forgot 'yield return'?");
				return result;
			}
		}

		abstract public void ExecuteStep(Action cont);
	}

	public class AsyncPrimitive<T> : Async<T> {
		Action<Action<T>> func;

		public AsyncPrimitive(Action<Action<T>> function) {
			this.func = function;
		}

		public AsyncPrimitive(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, T> end) {
			this.func = (cont) => begin(delegate (IAsyncResult res) {
				Console.WriteLine("callback: Current Thread Id :{0}", Thread.CurrentThread.ManagedThreadId);

				cont(end(res));
			}, null);
		}

		public override void ExecuteStep(Action cont) {
			func((res) => {
				result = res;
				completed = true;
				cont();
			});
		}
	}

	public class AsyncWithResult<T> : Async<T> {
		IEnumerable<IAsync> en;

		public AsyncWithResult(IEnumerable<IAsync> async) {
			en = async;
		}

		public override void ExecuteStep(Action cont) {
			AsyncExtensions.Run<T>(en.GetEnumerator(), (res) => {
				completed = true;
				result = res;
				cont();
			});
		}
	}

	public class AsyncWithUnitResult : Async<Unit> {
		IEnumerable<IAsync> en;

		public AsyncWithUnitResult(IEnumerable<IAsync> async) {
			en = async;
			result = Unit.Value;
		}

		public override void ExecuteStep(Action cont) {
			AsyncExtensions.Run(en.GetEnumerator(), () => {
				completed = true;
				cont();
			});
		}
	}
}
