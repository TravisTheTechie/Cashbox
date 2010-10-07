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
	using Magnum.Extensions;
	using Magnum.Serialization;
	using Messages;


	public class CashboxDocumentSession :
		DocumentSession
	{
		readonly FastTextSerializer _serializer = new FastTextSerializer();
		readonly Engine _engine;

		public CashboxDocumentSession(Engine engine)
		{
			_engine = engine;
			MakeRequest<Startup, string>(new Startup());
		}

		public T Retrieve<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);

			string text = MakeRequest<RetrieveValue, string>(new RetrieveValue
				{
					Key = realKey
				});

			if (text == null)
				return default(T);

			return _serializer.Deserialize<T>(text);
		}

		public T RetrieveWithDefault<T>(string key, Func<T> defaultCreation) where T : class
		{
			string realKey = KeyConverter<T>(key);

			string text = MakeRequest<RetrieveValue, string>(new RetrieveValue
				{
					Key = realKey
				});

			if (string.IsNullOrEmpty(text))
			{
				T defaultValue = defaultCreation();
				string defaultText = _serializer.Serialize(defaultValue);
				Send(new StoreValue { Key = realKey, Value = defaultText });
				return defaultValue;
			}

			return _serializer.Deserialize<T>(text);
		}

		public void Store<T>(string key, T document) where T : class
		{
			string realKey = KeyConverter<T>(key);
			string text = _serializer.Serialize(document);

			Send(new StoreValue
				{
					Key = realKey,
					Value = text
				});
		}

		public IEnumerable<T> List<T>() where T : class
		{
			string keyStart = KeyConverter<T>(string.Empty);
			List<string> values = null;

			values = MakeRequest<ListValuesForType, List<string>>(new ListValuesForType
				{
					Key = keyStart
				});

			if (values == null)
				values = new List<string>();

			return values.ConvertAll(str => _serializer.Deserialize<T>(str));
		}

		public void Delete<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);

			Send(new RemoveValue
				{
					Key = realKey
				});
		}

		TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			return _engine.MakeRequest<TRequest, TResponse>(message);
		}

		void Send<T>(T message) where T : CashboxMessage
		{
			_engine.Send(message);
		}

		public void Dispose()
		{
			MakeRequest<Shutdown, string>(new Shutdown());
			_engine.Dispose();
		}

		static string KeyConverter<T>(string key)
		{
			return "{0}___{1}".FormatWith(typeof(T).FullName, key);
		}
	}
}