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
	using System.Linq;


	public class TypeCabin
	{
		readonly Dictionary<string, object> _untypedStore = new Dictionary<string, object>();
		Type _type;

		public TypeCabin(Type type)
		{
			_type = type;
		}

		public object this[string key]
		{
			get { return _untypedStore[key]; }
			set { _untypedStore[key] = value; }
		}

		public bool Contains(string key)
		{
			return _untypedStore.ContainsKey(key);
		}

		public void Add<T>(string key, T document)
		{
			_untypedStore.Add(key, document);
		}

		public IList<T> GetValues<T>() where T : class
		{
			return _untypedStore.Values.ToList().ConvertAll(x => x as T);
		}

		public void Delete(string key)
		{
			_untypedStore.Remove(key);
		}
	}
}