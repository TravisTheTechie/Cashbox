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
		readonly Engine _engine;
		readonly FastTextSerializer _serializer = new FastTextSerializer();

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
				Send(new StoreValue
					{
						Key = realKey,
						Value = defaultText
					});
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

		public void Dispose()
		{
			_engine.Dispose();
		}

		TResponse MakeRequest<TRequest, TResponse>(TRequest message) where TRequest : CashboxMessage
		{
			return _engine.MakeRequest<TRequest, TResponse>(message);
		}

		void Send<T>(T message) where T : CashboxMessage
		{
			_engine.Send(message);
		}

		static string KeyConverter<T>(string key)
		{
			return "{0}___{1}".FormatWith(typeof(T).FullName, key);
		}
	}
}