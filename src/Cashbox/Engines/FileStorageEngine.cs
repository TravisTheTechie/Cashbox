// Copyright (c) 2010 Travis Smith
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
namespace Cashbox.Engines
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;
	using FileStorage;
	using log4net;
	using Magnum.Reflection;
	using Magnum.Serialization;
	using Messages;
	using Stact;
	using Stact.Internal;


	public class FileStorageEngine :
		Engine
	{
		static readonly ILog _logger = LogManager.GetLogger("Cashbox.Engines.SqliteEngine");

		readonly Fiber _fiber = new ThreadFiber();
		readonly ChannelAdapter _input = new ChannelAdapter();
		readonly ChannelConnection _subscription;

		protected static readonly FastTextSerializer Serializer = new FastTextSerializer();

		readonly StreamStorage _storage;

		// Private methods to handle some oddities we're seeing in Magnum
		string Serialize<T>(T obj) { return Serializer.Serialize<T>(obj); }
		object Deserialize<T>(string text) { return Serializer.Deserialize<T>(text); }

		public FileStorageEngine(string filename)
		{
			_storage = new StreamStorage(new FileStream(filename, FileMode.OpenOrCreate));

			_subscription = _input.Connect(config =>
			{
				config.AddConsumerOf<Request<Startup>>()
					.UsingConsumer(Startup)
					.HandleOnFiber(_fiber);

				config.AddConsumerOf<RemoveValue>()
					.UsingConsumer(RemoveKeyFromSession)
					.HandleOnFiber(_fiber);

				config.AddConsumerOf<Request<RetrieveValue>>()
					.UsingConsumer(RetrieveValue)
					.HandleOnFiber(_fiber);

				config.AddConsumerOf<Request<ListValuesForType>>()
					.UsingConsumer(RetrieveListFromType)
					.HandleOnFiber(_fiber);

				config.AddConsumerOf<StoreValue>()
					.UsingConsumer(StoreValue)
					.HandleOnFiber(_fiber);
			});
		}

		void StoreValue(StoreValue message)
		{
			var json = this.FastInvoke<FileStorageEngine, string>(new[] { message.DocumentType }, "Serialize", message.Value);

			_storage.Store(message.DocumentType.ToString(), message.Key, Encoding.UTF8.GetBytes(json));
		}

		void RetrieveListFromType(Request<ListValuesForType> message)
		{
			try
			{
				var values = _storage
					.Keys
					.Where(x => x.First == message.Body.DocumentType.ToString())
					.ToList()
					.ConvertAll(x => GetValue(message.Body.DocumentType, x.Second));
				
				Respond(message, values);
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}

		}

		void RetrieveValue(Request<RetrieveValue> message)
		{
			try
			{
				object value = GetValue(message.Body.DocumentType, message.Body.Key);

				Respond(message, value);
			}
			catch (Exception ex)
			{
				RespondWithException(message, ex);
			}
		}

		object GetValue(Type table, string key)
		{
			var bytes = _storage.Read(table.ToString(), key);
			if (bytes == null)
				return null;
			var json = Encoding.UTF8.GetString(bytes);
			return this.FastInvoke<FileStorageEngine, object>(new[] { table }, "Deserialize", json);
		}

		void RemoveKeyFromSession(RemoveValue message)
		{
			_storage.Remove(message.DocumentType.ToString(), message.Key);
		}

		void Startup(Request<Startup> message)
		{
			Respond(message, string.Empty);
		}

		public void Dispose()
		{
			_fiber.Shutdown(TimeSpan.FromSeconds(120));
			_subscription.Disconnect();
			_storage.Dispose();
		}

		public TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			var response = new Magnum.Future<object>();
			var channel = new ChannelAdapter();
			Exception ex = null;

			using (channel.Connect(config =>
			{
				config.AddConsumerOf<ReturnValue>()
					.UsingConsumer(msg => response.Complete(msg.Value));

				config.AddConsumerOf<ReturnException>()
					.UsingConsumer(msg => ex = msg.Exception);
			}))
			{
				_input.Request(message, channel);

				if (!response.WaitUntilCompleted(TimeSpan.FromSeconds(180)) && ex != null)
					throw ex;

				if (response.Value == null)
					return default(TResponse);
				
				return (TResponse)response.Value;
			}
		}

		public void Send<T>(T message) where T : CashboxMessage
		{
			_input.Send(message);
		}

		static void Respond<T, TK>(Request<TK> message, T response)
		{
			message.ResponseChannel.Send(new ReturnValue
			{
				DocumentType = typeof(T),
				Value = response
			});
		}

		static void RespondWithException<T>(Request<T> message, Exception ex)
		{
			message.ResponseChannel.Send(new ReturnException
			{
				Exception = new SqliteEngineException(string.Format("Error with {0}", typeof(T).Name), ex)
			});
		}
	}
}