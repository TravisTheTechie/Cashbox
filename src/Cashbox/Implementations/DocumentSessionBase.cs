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
    using Magnum.Channels;
    using Magnum.Extensions;
    using Magnum.Fibers;
    using Magnum.Serialization;
    using Messages;


    public class DocumentSessionBase :
        IDisposable
    {
        protected static readonly FastTextSerializer Serializer = new FastTextSerializer();
        readonly Fiber _fiber = new SynchronousFiber();
        readonly string _filename;
        readonly ChannelAdapter _input = new ChannelAdapter();
        readonly Action<Action> _needLockin;
        readonly ChannelConnection _subscription;

        ManualResetEvent _saveCompleted;
        Dictionary<string, string> _store = new Dictionary<string, string>();

        public DocumentSessionBase(string filename)
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
                    config.AddConsumerOf<IoProducer>()
                        .BufferFor(250.Milliseconds()) // quarter of a sec
                        .UsingConsumer(msgs => Save())
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<RemoveKey>()
                        .UsingConsumer(RemoveKeyFromSession)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<LoadFromDisk>()
                        .UsingConsumer(msg => Load())
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<Request<LoadFromDisk>>()
                        .UsingConsumer(LoadWithAcknolwedgement)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<Request<GetWithKey>>()
                        .UsingConsumer(RetrieveValue)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<Request<GetWithKeyAndDefault>>()
                        .UsingConsumer(RetrieveValueWithDefault)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<Request<GetListWithType>>()
                        .UsingConsumer(RetrieveListFromType)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<StoreWithKeyAndValue>()
                        .UsingConsumer(StoreValue)
                        .HandleOnFiber(_fiber);

                    config.AddConsumerOf<SaveToDisk>()
                        .UsingConsumer(msg => Save())
                        .HandleOnFiber(_fiber);
                });
        }

        public void Dispose()
        {
            using (_saveCompleted = new ManualResetEvent(false))
            {
                RegisterIoEvent(string.Empty);
                _saveCompleted.WaitOne(15.Seconds());
                _subscription.Disconnect();
            }
            _saveCompleted = null;
        }

        void LoadWithAcknolwedgement(Request<LoadFromDisk> message)
        {
            Load();
            message.ResponseChannel.Send(new ReturnValue<string>());
        }

        void RetrieveListFromType(Request<GetListWithType> message)
        {
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

        protected void Send<T>(T message)
        {
            _input.Send(message);
        }

        protected void Request<T>(T message, UntypedChannel channel)
        {
            _input.Request(message, channel);
        }

        void RetrieveValueWithDefault(Request<GetWithKeyAndDefault> message)
        {
            string text = null;

            _needLockin(() =>
                {
                    if (!_store.ContainsKey(message.Body.Key))
                    {
                        _store.Add(message.Body.Key, message.Body.DefaultValue);
                        _input.Send(new IoProducer
                            {
                                Key = message.Body.Key
                            });
                    }
                    text = _store[message.Body.Key];
                });

            message.ResponseChannel.Send(new ReturnValue<string>
                {
                    Key = message.Body.Key,
                    Value = text
                });
        }

        void RetrieveValue(Request<GetWithKey> message)
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

        void RemoveKeyFromSession(RemoveKey message)
        {
            _needLockin(() => { _store.Remove(message.Key); });
            RegisterIoEvent(message.Key);
        }

        void RegisterIoEvent(string key)
        {
            _input.Send(new IoProducer
                {
                    Key = key
                });
        }

        void StoreValue(StoreWithKeyAndValue message)
        {
            _needLockin(() =>
                {
                    if (!_store.ContainsKey(message.Key))
                        _store.Add(message.Key, message.Value);
                    else
                        _store[message.Key] = message.Value;
                });

            RegisterIoEvent(message.Key);
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