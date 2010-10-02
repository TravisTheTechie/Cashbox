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
	using System.Linq;
	using Magnum.Serialization;

	
	public class TypeCabin
	{
		readonly Dictionary<string, string> _untypedStore = new Dictionary<string, string>();
		[ThreadStatic]
		static FastTextSerializer _serializer = new FastTextSerializer();

		public bool Contains(string key)
		{
			return _untypedStore.ContainsKey(key);
		}

		public void Add<T>(string key, T document)
		{
			_untypedStore.Add(key, _serializer.Serialize<T>(document));
		}

		public IList<T> GetValues<T>() where T : class
		{
			return _untypedStore.Values.ToList().ConvertAll(x => _serializer.Deserialize<T>(x));
		}
		
		public T Retrieve<T>(string key) where T : class
		{
			return _serializer.Deserialize<T>(_untypedStore[key]);
		}
		
		public void Store<T>(string key, T document)
		{
			_untypedStore[key] = _serializer.Serialize<T>(document);
		}

		public void Delete(string key)
		{
			_untypedStore.Remove(key);
		}
	}
}