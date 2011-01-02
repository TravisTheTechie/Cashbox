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
		RecordHeader _readHeader;
		RecordHeader _storedHeader;
		long _streamLocation;
		long _targetSize;

		[When]
		public void Store_is_called_with_data()
		{
			byte[] data = BitConverter.GetBytes(0);

			_storedHeader = new RecordHeader
				{
					HeaderVersion = 1,
					RecordSize = data.Length,
					Action = StorageActions.Store
				};

			// the "size" of RecordHeader will have to managed here
			_targetSize = data.Length + sizeof(Int32)*3;

			_streamLocation = Storage.Store(_storedHeader, data);

			DataStream.SeekStart();
			_readHeader = DataStream.ReadRecordHeader();
		}

		[Then]
		public void Stream_location_should_be_header_plus_data()
		{
			_streamLocation.ShouldEqual(_targetSize);
		}

		[Then]
		public void Read_header_should_equal_stored_header()
		{
			_readHeader.HeaderVersion.ShouldEqual(_storedHeader.HeaderVersion);
			_readHeader.RecordSize.ShouldEqual(_storedHeader.RecordSize);
			_readHeader.Action.ShouldEqual(_storedHeader.Action);
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

			DataStream.WriteStreamHeader(new StreamHeader());

			Storage.Store(new RecordHeader
				{
					Action = StorageActions.Store
				}, BitConverter.GetBytes(1));

			Storage.Store(new RecordHeader
				{
					Action = StorageActions.Store
				}, BitConverter.GetBytes(2));

			Storage.Store(new RecordHeader
				{
					Action = StorageActions.Store
				}, BitConverter.GetBytes(3));

			Storage.Store(new RecordHeader
				{
					Action = StorageActions.Store
				}, BitConverter.GetBytes(4));

			// don't have a key stored yet...
			//  -- besides record length we need a key length and read that data block as well when reading the index data
			//  -- record headers should likely contain the record start location in the stream
			//  -- still need to consider table support, are we going to support tables-per-type here?
		}
	}
}