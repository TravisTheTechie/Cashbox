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
namespace Cashbox.Specs.FileStorage
{
	using System;
	using System.IO;
	using Engines.FileStorage;
	using Magnum.TestFramework;


	public class StreamStorageSpecsBase
	{
		protected string Table { get; set; }
		protected StreamStorage Storage { get; set; }

		protected MemoryStream DataStream { get; set; }

		[Given]
		public void A_stream_storage_instance()
		{
			if (DataStream == null)
				DataStream = new MemoryStream();

			Storage = new StreamStorage(DataStream);

			Table = "Test";
		}
	}


	[Scenario]
	public class Data_stream_contains_new_storage_header :
		StreamStorageSpecsBase
	{
		[Then]
		public void Storage_has_populated_header_property()
		{
			Storage.Header.Version.ShouldEqual(1);
		}
	}


	[Scenario]
	public class Can_build_index_from_stream :
		StreamStorageSpecsBase
	{
		[When]
		public void Build_index_called_on_populated_store()
		{
			// Store a set of ints: 1, 2, 3, 4

			Storage.Store(Table, "One", BitConverter.GetBytes(1));

			Storage.Store(Table, "Two", BitConverter.GetBytes(2));

			Storage.Store(Table, "Three", BitConverter.GetBytes(3l));

			Storage.Store(Table, "Four", BitConverter.GetBytes(4));
		}

		[Then]
		public void One_should_return_1()
		{
			Storage.Read(Table, "One").ShouldEqual(BitConverter.GetBytes(1));
		}

		[Then]
		public void Two_should_return_2()
		{
			Storage.Read(Table, "Two").ShouldEqual(BitConverter.GetBytes(2));
		}

		[Then]
		public void Three_should_return_3_as_a_long()
		{
			Storage.Read(Table, "Three").ShouldEqual(BitConverter.GetBytes(3l));
		}

		[Then]
		public void Four_should_return_4()
		{
			Storage.Read(Table, "Four").ShouldEqual(BitConverter.GetBytes(4));
		}
	}
}