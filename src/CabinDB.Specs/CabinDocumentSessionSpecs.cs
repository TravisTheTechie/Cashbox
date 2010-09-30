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
namespace CabinDB.Specs
{
	using System.Linq;
	using Magnum.TestFramework;


	[Scenario]
	public class CabinDocumentSessionBase
	{
		protected CabinDocumentSession Session { get; set; }

		[Given]
		public void A_cabin_session()
		{
			Session = new CabinDocumentSession();
		}
	}


	[Scenario]
	public class A_value_is_storable :
		CabinDocumentSessionBase
	{
		string _key = "empty";
		
		[When]
		public void A_value_is_stored()
		{
			Session.Store(_key, new TestDocument1());
		}

		[Then]
		public void It_should_be_retrievable()
		{
			Session.Retrieve<TestDocument1>(_key).ShouldNotBeNull();
		}

		[Then]
		public void List_should_have_one_value()
		{
			Session.List<TestDocument1>().Count().ShouldEqual(1);
		}
	}
}