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
	using Messages;


	public class CashboxDocumentSession :
		DocumentSession
	{
		readonly Engine _engine;

		public CashboxDocumentSession(Engine engine)
		{
			_engine = engine;
			MakeRequest<Startup, string>(new Startup());
		}

		public T Retrieve<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);

			var value = MakeRequest<RetrieveValue, object>(new RetrieveValue
				{
					Key = realKey,
                    DocumentType = typeof(T)
				});

			if (value == null)
				return default(T);

		    return value as T;
		}

		public T RetrieveWithDefault<T>(string key, Func<T> defaultCreation) where T : class
		{
			string realKey = KeyConverter<T>(key);

			var value = MakeRequest<RetrieveValue, object>(new RetrieveValue
				{
					DocumentType = typeof(T),
					Key = realKey
				});

			if (value == null)
			{
				T defaultValue = defaultCreation();
				Send(new StoreValue
					{
						Key = realKey,
						Value = defaultValue,
                        DocumentType = typeof(T)
					});
				return defaultValue;
			}

		    return value as T;
		}

		public void Store<T>(string key, T document) where T : class
		{
			string realKey = KeyConverter<T>(key);

			Send(new StoreValue
				{
					Key = realKey,
					Value = document,
                    DocumentType = typeof(T)
                 
				});
		}

		public IEnumerable<T> List<T>() where T : class
		{
			string keyStart = KeyConverter<T>(string.Empty);
			
			List<object> values = MakeRequest<ListValuesForType, List<object>>(new ListValuesForType
				{
					Key = keyStart,
                    DocumentType = typeof(T)
				});

            if (values == null)
                return new List<T>();

		    return values.ConvertAll(value => value as T);
		}

		public void Delete<T>(string key) where T : class
		{
			string realKey = KeyConverter<T>(key);

			Send(new RemoveValue
				{
					DocumentType = typeof(T),
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
			return key;
		}
	}
}