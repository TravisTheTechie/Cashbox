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
using System.Linq.Expressions;
using Magnum.Reflection;

namespace Cashbox.Engines
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using log4net;
	using Magnum.Extensions;
	using Magnum.Serialization;
	using Messages;
	using Stact;
	using Stact.Internal;


	public class InMemoryEngine :
		Engine
	{
		protected static readonly FastTextSerializer Serializer = new FastTextSerializer();
		static readonly ILog _logger = LogManager.GetLogger("Cashbox.Engines.InMemoryEngine");
		readonly Fiber _fiber = new ThreadFiber();
		readonly ChannelAdapter _input = new ChannelAdapter();
		readonly Action<Action> _needLockin;
		readonly ChannelConnection _subscription;
		string _filename;

		ManualResetEvent _saveCompleted;
		Dictionary<string, string> _store = new Dictionary<string, string>();

        // Private methods to handle some oddities we're seeing in Magnum
        // ReSharper disable UnusedMember.Local
        string Serialize<T>(T obj) { return Serializer.Serialize<T>(obj); }
        object Deserialize<T>(string text) { return Serializer.Deserialize<T>(text); }
        // ReSharper restore UnusedMember.Local

		public InMemoryEngine(string filename)
		{
			_filename = filename;

			_needLockin = act =>
				{
					lock (_store)
					{
						act();
					}
				};

			_subscription = _input.Connect(config =>
				{
					config.AddConsumerOf<Request<Startup>>()
						.UsingConsumer(msg =>
							{
								Load();
								msg.ResponseChannel.Send(new ReturnValue());
							})
						.HandleOnCallingThread();

					config.AddConsumerOf<InMemoryEngineDataChange>()
						.BufferFor(250.Milliseconds()) // quarter of a sec
						.UsingConsumer(msgs => Save())
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

		public void Dispose()
		{
			using (_saveCompleted = new ManualResetEvent(false))
			{
				RegisterMemoryChange(string.Empty);
				_saveCompleted.WaitOne(15.Seconds());
				_fiber.Shutdown(45.Seconds());
				_subscription.Disconnect();
			}
			_saveCompleted = null;
		}

		public TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			var response = new Magnum.Future<TResponse>();
			var channel = new ChannelAdapter();

			using (channel.Connect(config =>
				{
					config.AddConsumerOf<ReturnValue>()
						.UsingConsumer(msg => response.Complete((TResponse)msg.Value));
				}))
			{
				_input.Request(message, channel);

				response.WaitUntilCompleted(5.Seconds());
				return response.Value;
			}
		}

		public void Send<T>(T message) where T : CashboxMessage
		{
			_input.Send(message);
		}

		void RetrieveListFromType(Request<ListValuesForType> message)
		{
			List<object> values = null;

			_needLockin(() =>
				{
					values = _store
						.Where(kvp => kvp.Key.StartsWith(message.Body.Key))
						.Select(kvp => kvp.Value )
                        .Select(str => this.FastInvoke<InMemoryEngine, object>(new[] {message.Body.DocumentType}, "Deserialize", str))
						.ToList();
				});



			message.ResponseChannel.Send(new ReturnValue
				{
					Value = values
				});
		}

		public void SetFilename(string filename)
		{
			_filename = filename;
		}

		void RetrieveValue(Request<RetrieveValue> message)
		{
			object text = null;

			_needLockin(() =>
				{
					if (_store.ContainsKey(message.Body.Key))
						text = _store[message.Body.Key];
				});

			message.ResponseChannel.Send(new ReturnValue
				{
					Key = message.Body.Key,
                    Value = this.FastInvoke<InMemoryEngine, object>(new[] { message.Body.DocumentType }, "Deserialize", text) 
				});
		}

		void RemoveKeyFromSession(RemoveValue message)
		{
			_needLockin(() => _store.Remove(message.Key));
			RegisterMemoryChange(message.Key);
		}

		void RegisterMemoryChange(string key)
		{
			_input.Send(new InMemoryEngineDataChange
				{
					Key = key
				});
		}

		void StoreValue(StoreValue message)
		{
		    var serializedValue = this.FastInvoke<InMemoryEngine, string>(new[] {message.DocumentType}, "Serialize", message.Value);

			_needLockin(() =>
				{
					if (!_store.ContainsKey(message.Key))
                        _store.Add(message.Key, serializedValue);
					else
                        _store[message.Key] = serializedValue;
				});

			RegisterMemoryChange(message.Key);
		}

		void Load()
		{
			if (File.Exists(_filename))
			{
				string value = File.ReadAllText(_filename);
				_needLockin(() =>
				{
				    _store = Serializer.Deserialize<Dictionary<string, string>>(value);
				});
			}
		}

		void Save()
		{
			string text = string.Empty;
			_needLockin(() => text = Serializer.Serialize(_store));
			File.WriteAllText(_filename, text);
			if (_saveCompleted != null)
				_saveCompleted.Set();
		}
	}
}