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
namespace Cashbox
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Magnum.Channels;
	using Magnum.Extensions;
	using Magnum.Serialization;


	public class DocumentSession :
		IDocumentSession
	{
		static readonly FastTextSerializer _serializer = new FastTextSerializer();
		readonly string _filename;
		readonly ChannelAdapter _input;
		readonly ChannelConnection _subscription;
		Dictionary<string, string> _store = new Dictionary<string, string>();

		public DocumentSession(string filename)
		{
			_filename = filename;
			Load();

			_input = new ChannelAdapter();

			_subscription = _input.Connect(config =>
				{
					config.AddConsumerOf<IoProducer>()
						.BufferFor(250.Milliseconds()) // quarter of a sec
						.UsingConsumer(msgs => Save());
				});
		}

		public T Retrieve<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);
			string text;

			lock (_store)
			{
				if (!_store.ContainsKey(realKey))
					return default(T);

				text = _store[realKey];
			}

			return _serializer.Deserialize<T>(text);
		}

		public T RetrieveWithDefault<T>(string key, Func<T> defaultCreation) where T : class
		{
			string realKey = KeyConverter<T>(key);
			string text;

			lock (_store)
			{
				if (!_store.ContainsKey(realKey))
				{
					_store.Add(realKey, _serializer.Serialize(defaultCreation()));
					RegisterIoEvent();
				}

				text = _store[realKey];
			}

			return _serializer.Deserialize<T>(text);
		}

		public void Store<T>(string key, T document) where T : class
		{
			string realKey = KeyConverter<T>(key);
			string text = _serializer.Serialize(document);

			lock (_store)
			{
				if (!_store.ContainsKey(realKey))
					_store.Add(realKey, text);
				else
					_store[realKey] = text;
			}
		
			RegisterIoEvent();
		}

		public IEnumerable<T> List<T>() where T : class
		{
			string keyStart = KeyConverter<T>(string.Empty);

			lock (_store)
			{
				return _store
					.Where(kvp => kvp.Key.StartsWith(keyStart))
					.Select(kvp => _serializer.Deserialize<T>(kvp.Value))
					.ToList();
			}
		}

		public void Delete<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);

			lock (_store)
			{
				if (!_store.ContainsKey(realKey))
					return;

				_store.Remove(realKey);
			}

			RegisterIoEvent();
		}

		public void Dispose()
		{
			_subscription.Disconnect();
			Save();
		}

		void RegisterIoEvent()
		{
			_input.Send(new IoProducer());
		}

		void Load()
		{
			if (File.Exists(_filename))
			{
				string value = File.ReadAllText(_filename);
				_store = _serializer.Deserialize<Dictionary<string, string>>(value);
			}
		}

		void Save()
		{
			string text;
			lock (_store)
			{
				text = _serializer.Serialize(_store);
			}
			File.WriteAllText(_filename, text);
		}

		static string KeyConverter<T>(string key)
		{
			return "{0}___{1}".FormatWith(typeof(T).FullName, key);
		}
	}


	class IoProducer
	{
	}
}