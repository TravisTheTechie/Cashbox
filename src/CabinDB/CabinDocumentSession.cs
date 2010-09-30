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
namespace CabinDB
{
	using System;
	using System.Collections.Generic;


	public class CabinDocumentSession :
		IDocumentSession
	{
		readonly Dictionary<Type, TypeCabin> _store = new Dictionary<Type, TypeCabin>();

		public T Retrieve<T>(string key) where T : class
		{
			TypeCabin typeCabin;

			lock (_store)
			{
				if (!_store.ContainsKey(typeof(T)))
					return default(T);

				typeCabin = _store[typeof(T)];

				if (!typeCabin.Contains(key))
					return default(T);

				return typeCabin[key] as T;
			}
		}

		public T RetrieveWithDefault<T>(string key, Func<T> defaultCreation) where T : class
		{
			throw new NotImplementedException();
		}

		public void Store<T>(string key, T document) where T : class
		{
			TypeCabin typeCabin;

			lock (_store)
			{
				if (!_store.ContainsKey(typeof(T)))
					_store.Add(typeof(T), new TypeCabin(typeof(T)));

				typeCabin = _store[typeof(T)];

				if (!typeCabin.Contains(key))
					typeCabin.Add(key, document);
				else
					typeCabin[key] = document;
			}
		}

		public IEnumerable<T> List<T>() where T : class
		{
			lock (_store)
			{
				if (_store.ContainsKey(typeof(T)))
					return _store[typeof(T)].GetValues<T>();

				return new List<T>();
			}
		}

		public void Delete(string key)
		{
			throw new NotImplementedException();
		}
	}
}