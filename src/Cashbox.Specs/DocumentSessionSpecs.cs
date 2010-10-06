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
namespace Cashbox.Specs
{
	using System.Linq;
	using Implementations;
	using Magnum.TestFramework;

	
	[Scenario]
	public class DocumentSessionBase
	{
		protected DocumentSession Session { get; set; }

		[Given]
		public void A_cabin_session()
		{
			Session = new DocumentSession("session.specs.store");
		}

        [Finally]
        public void Dispose_of_stuff()
        {
            Session.Dispose();
        }
	}


	[Scenario]
	public class A_value_is_storable :
		DocumentSessionBase
	{
		const string Key = "empty";

		[When]
		public void A_value_is_stored()
		{
			Session.Store(Key, new TestDocument1());
		}

		[Then]
		public void It_should_be_retrievable()
		{
			Session.Retrieve<TestDocument1>(Key).ShouldNotBeNull();
		}

		[Then]
		public void List_should_have_one_value()
		{
			Session.List<TestDocument1>().Count().ShouldEqual(1);
		}

        [After]
        public void Clear_out_list_values()
        {
            Session.Delete<TestDocument1>(Key);
        }
	}


	[Scenario]
	public class Default_retrieves_store_values_when_missing :
		DocumentSessionBase
	{
		const string Key = "za-za";

		[When]
		public void A_miss_with_retrieve_with_default()
		{
			Session.Retrieve<TestDocument1>(Key).ShouldBeNull();

			Session.RetrieveWithDefault(Key, () => new TestDocument1());
		}

		[Then]
		public void It_should_be_retrievable()
		{
			Session.Retrieve<TestDocument1>(Key).ShouldNotBeNull();
		}

		[After]
		public void Clear_out_list_values()
		{
			Session.Delete<TestDocument1>(Key);
		}
	}


	[Scenario]
	public class Delete_will_remove_an_existing_value :
		DocumentSessionBase
	{
		const string Key = "di-da";

		[When]
		public void An_existing_value_is_deleted()
		{
			Session.Store(Key, new TestDocument1());

			Session.Retrieve<TestDocument1>(Key).ShouldNotBeNull();

			Session.Delete<TestDocument1>(Key);
		}

		[Then]
		public void It_should_have_a_miss_when_retrieving()
		{
			Session.Retrieve<TestDocument1>(Key).ShouldBeNull();
		}
	}
}