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
    using Magnum;
    using Magnum.Channels;
    using Magnum.Extensions;
    using Messages;


    public class DocumentSession : 
        DocumentSessionBase,
        IDocumentSession
    {
        public DocumentSession(string filename) :
            base(filename)
        {
            Send(new LoadFromDisk());
        }

        public T Retrieve<T>(string key) where T : class
        {
            string realKey = KeyConverter<T>(key);
            string text = null;

            var channel = new ChannelAdapter();
            var response = new Future<string>();

            using (channel.Connect(config =>
                {
                    config.AddConsumerOf<ReturnValue<string>>()
                        .UsingConsumer(msg => response.Complete(msg.Value));
                }))
            {
                Request(new GetWithKey
                    {
                        Key = realKey
                    }, channel);

                response.WaitUntilCompleted(1.Minutes());
                text = response.Value;
            }

            if (text == null)
                return default(T);

            return Serializer.Deserialize<T>(text);
        }

        public T RetrieveWithDefault<T>(string key, Func<T> defaultCreation) where T : class
        {
            string realKey = KeyConverter<T>(key);
            string text = null;
            string defaultValue = Serializer.Serialize(defaultCreation());
            var response = new Future<string>();
            var channel = new ChannelAdapter();

            using (channel.Connect(config =>
                {
                    config.AddConsumerOf<ReturnValue<string>>()
                        .UsingConsumer(msg => response.Complete(msg.Value));
                }))
            {
                Request(new GetWithKeyAndDefault
                    {
                        Key = realKey,
                        DefaultValue = defaultValue
                    }, channel);

                response.WaitUntilCompleted(1.Minutes());
                text = response.Value;
            }

            return Serializer.Deserialize<T>(text);
        }

        public void Store<T>(string key, T document) where T : class
        {
            string realKey = KeyConverter<T>(key);
            string text = Serializer.Serialize(document);

            Send(new StoreWithKeyAndValue
                {
                    Key = realKey,
                    Value = text
                });
        }

        public IEnumerable<T> List<T>() where T : class
        {
            string keyStart = KeyConverter<T>(string.Empty);
            List<string> values = null;

            var response = new Future<List<string>>();
            var channel = new ChannelAdapter();

            using (channel.Connect(config =>
                {
                    config.AddConsumerOf<ReturnValue<List<string>>>()
                        .UsingConsumer(msg => response.Complete(msg.Value));
                }))
            {
                Request(new GetListWithType
                    {
                        Key = keyStart
                    }, channel);

                response.WaitUntilCompleted(1.Minutes());
                values = response.Value;
            }

            if (values == null)
                values = new List<string>();

            return values.ConvertAll(str => Serializer.Deserialize<T>(str));
        }

        public void Delete<T>(string key) where T : class
        {
            string realKey = KeyConverter<T>(key);

            Send(new RemoveKey
                {
                    Key = realKey
                });
        }

        static string KeyConverter<T>(string key)
        {
            return "{0}___{1}".FormatWith(typeof(T).FullName, key);
        }
    }
}