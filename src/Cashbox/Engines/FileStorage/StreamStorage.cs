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
namespace Cashbox.Engines.FileStorage
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Magnum.Collections;


	public class StreamStorage :
		IDisposable
	{
		readonly Stream _dataStream;

		readonly Dictionary<Tuple<string, string>, RecordHeader> _indexes =
			new Dictionary<Tuple<string, string>, RecordHeader>();

		public StreamStorage(Stream dataStream)
		{
			_dataStream = dataStream;

			// reset stream to the start
			_dataStream.SeekStart();

			if (_dataStream.Length > 0)
				ReadIndex();
			else
				WriteNewHeader();
		}

		public StreamHeader Header { get; set; }

		public Tuple<string, string>[] Keys
		{
			get { return _indexes.Keys.ToArray(); }
		}

		public void Dispose()
		{
			_dataStream.Close();
		}

		void WriteNewHeader()
		{
			Header = new StreamHeader
				{
					Version = 1
				};

			_dataStream.WriteStreamHeader(Header);
		}

		void LoadHeader()
		{
			Header = _dataStream.ReadStreamHeader();
		}

		public void Store(string table, string key, byte[] data)
		{
			var header = new RecordHeader
				{
					Key = key,
					Table = table,
					Action = StorageActions.Store,
					RecordSize = data.Length
				};

			_dataStream.SeekEnd();
			_dataStream.WriteRecordHeader(header);

			header.RecordLocation = _dataStream.Position;

			_dataStream.Write(data);

			IndexRecord(header);
		}

		public void Remove(string table, string key)
		{
			var header = new RecordHeader
				{
					Action = StorageActions.Delete,
					RecordSize = 0,
					Table = table,
					Key = key
				};

			_dataStream.SeekEnd();
			_dataStream.WriteRecordHeader(header);

			header.RecordLocation = _dataStream.Position;

			IndexRecord(header);
		}

		public void ReadIndex()
		{
			LoadHeader();

			while (_dataStream.Position < _dataStream.Length)
			{
				RecordHeader header = _dataStream.ReadRecordHeader();
				if (header != null)
					IndexRecord(header);
			}
		}

		void IndexRecord(RecordHeader header)
		{
			var key = new Tuple<string, string>(header.Table, header.Key);

			if (header.Action == StorageActions.Store)
			{
				if (!_indexes.ContainsKey(key))
					_indexes.Add(key, header);
				else
					_indexes[key] = header;

				_dataStream.MovePositionForward(header.RecordSize);
			}
			else if (header.Action == StorageActions.Delete)
			{
				if (_indexes.ContainsKey(key))
					_indexes.Remove(key);
			}
		}

		public byte[] Read(string table, string itemKey)
		{
			var key = new Tuple<string, string>(table, itemKey);

			if (_indexes.ContainsKey(key))
			{
				RecordHeader header = _indexes[key];

				_dataStream.SeekLocation(header.RecordLocation);
				return _dataStream.Read(header.RecordSize);
			}

			return null;
		}
	}
}