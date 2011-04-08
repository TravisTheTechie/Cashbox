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
namespace Cashbox.Specs
{
	using System.IO;
	using System.Linq;
	using Engines;
	using Magnum.TestFramework;


	public class DocumentSessionBase
	{
		protected DocumentSession Session { get; set; }

		[Given]
		public void A_cabin_session()
		{
			DocumentSessionFactory.SetEngineFactory(str => new FileStorageEngine(str));
			Session = DocumentSessionFactory.Create("session.specs.store");
		}

		[Finally]
		public void Dispose_of_stuff()
		{
			Session.Dispose();
			File.Delete("session.specs.store");
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