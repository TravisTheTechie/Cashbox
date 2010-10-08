// Copyright 2010 Travis Smith
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Cashbox.Implementations
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
        static readonly ILog _logger = LogManager.GetLogger("Cashbox.Engine.InMemoryEngine");

        protected static readonly FastTextSerializer Serializer = new FastTextSerializer();
        readonly Fiber _fiber = new ThreadFiber();
        readonly ChannelAdapter _input = new ChannelAdapter();
        readonly Action<Action> _needLockin;
        readonly ChannelConnection _subscription;
    	string _filename;

        ManualResetEvent _saveCompleted;
        Dictionary<string, string> _store = new Dictionary<string, string>();

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
                		.UsingConsumer(msg => { Load(); msg.ResponseChannel.Send(new ReturnValue<string>()); })
                		.HandleOnFiber(_fiber);

					config.AddConsumerOf<Request<Shutdown>>()
						.UsingConsumer(msg => { Save(); msg.ResponseChannel.Send(new ReturnValue<string>()); })
						.HandleOnFiber(_fiber);

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
            //_logger.Info("Disposing");
            using (_saveCompleted = new ManualResetEvent(false))
            {
                RegisterMemoryChange(string.Empty);
                _saveCompleted.WaitOne(15.Seconds());
                _subscription.Disconnect();
            }
            _saveCompleted = null;
        }

    	void RetrieveListFromType(Request<ListValuesForType> message)
        {
            //_logger.DebugFormat("Retrieving list for {0}", message.Body.Key);

            List<string> values = null;

            _needLockin(() =>
                {
                    values = _store
                        .Where(kvp => kvp.Key.StartsWith(message.Body.Key))
                        .Select(kvp => kvp.Value)
                        .ToList();
                });

            message.ResponseChannel.Send(new ReturnValue<List<string>>
                {
                    Value = values
                });
        }

    	public TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
    	{
            var response = new Magnum.Future<TResponse>();
            var channel = new ChannelAdapter();

            using (channel.Connect(config =>
                {
                    config.AddConsumerOf<ReturnValue<TResponse>>()
                        .UsingConsumer(msg => response.Complete(msg.Value));
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

		public void SetFilename(string filename)
		{
			_filename = filename;
		}

        void RetrieveValue(Request<RetrieveValue> message)
        {
            string text = null;

            _needLockin(() =>
                {
                    if (_store.ContainsKey(message.Body.Key))
                        text = _store[message.Body.Key];
                });

            message.ResponseChannel.Send(new ReturnValue<string>
                {
                    Key = message.Body.Key,
                    Value = text
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
            _needLockin(() =>
                {
                    if (!_store.ContainsKey(message.Key))
                        _store.Add(message.Key, message.Value);
                    else
                        _store[message.Key] = message.Value;
                });

            RegisterMemoryChange(message.Key);
        }

        void Load()
        {
            if (File.Exists(_filename))
            {
                string value = File.ReadAllText(_filename);
                _needLockin(() => { _store = Serializer.Deserialize<Dictionary<string, string>>(value); });
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