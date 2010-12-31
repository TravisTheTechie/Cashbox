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
		protected StreamStorage Storage { get; set; }

		protected MemoryStream DataStream { get; set; }

		[Given]
		public void A_stream_storage_instance()
		{
			if (DataStream == null)
				DataStream = new MemoryStream();

			Storage = new StreamStorage(DataStream);
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
	public class Data_can_be_stored :
		StreamStorageSpecsBase
	{
		long _streamLocation;
		long _targetSize;

		[When]
		public void Store_is_called_with_data()
		{
			var data = new byte[] {0, 0, 0, 0};

			var header = new RecordHeader
				{
					HeaderVersion = 1,
					RecordSize = data.Length,
					Action = StorageActions.Store
				};

			// the "size" of RecordHeader will have to managed here
			_targetSize = data.Length + sizeof(Int32) * 3;

			_streamLocation = Storage.Store(header, data);
		}

		[Then]
		public void Stream_location_should_be_greater_than_zero()
		{
			_streamLocation.ShouldEqual(_targetSize);
		}
	}
}