// Copyright (c) 2010-2011 Travis Smith
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


    public class StreamStorage :
        IDisposable
    {
        readonly Dictionary<RecordKey, RecordHeader> _indexes =
            new Dictionary<RecordKey, RecordHeader>(new RecordKeyComparer());

        readonly Action<Stream> _primaryStreamCleanUp;
        readonly Func<Stream> _primaryStreamFactory;
        readonly Action<Stream> _tempStreamCleanUp;
        readonly Func<Stream> _tempStreamFactory;
        Stream _dataStream;

        public StreamStorage(Func<Stream> primaryStreamFactory, Func<Stream> tempStreamFactory,
                             Action<Stream> primaryStreamCleanUp, Action<Stream> tempStreamCleanUp)
        {
            _primaryStreamFactory = primaryStreamFactory;
            _tempStreamFactory = tempStreamFactory;
            _primaryStreamCleanUp = primaryStreamCleanUp;
            _tempStreamCleanUp = tempStreamCleanUp;

            _dataStream = _primaryStreamFactory();

            // reset stream to the start
            _dataStream.SeekStart();

            if (_dataStream.Length > 0)
                ReadIndex();
            else
                WriteNewHeader();
        }

        public StreamHeader Header { get; set; }

        public RecordKey[] Keys
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
            RecordHeader header = StoreToStream(_dataStream, key, table, data, StorageActions.Store);

            IndexRecord(header);
        }

        RecordHeader StoreToStream(Stream dataStream, string key, string table, byte[] data,
                                   StorageActions storageAction)
        {
            int recordSize = data == null ? 0 : data.Length;
            var header = new RecordHeader
                {
                    Key = key,
                    Table = table,
                    Action = storageAction,
                    RecordSize = recordSize
                };

            dataStream.SeekEnd();
            dataStream.WriteRecordHeader(header);

            header.RecordLocation = dataStream.Position;

            if (data != null)
                dataStream.Write(data);

            return header;
        }

        public void Remove(string table, string key)
        {
            RecordHeader header = StoreToStream(_dataStream, key, table, null, StorageActions.Delete);

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
            WriteHeaderToIndexForStream(header, _indexes, _dataStream);
        }

        void WriteHeaderToIndexForStream(RecordHeader header, Dictionary<RecordKey, RecordHeader> index,
                                         Stream stream)
        {
            var key = new RecordKey(header.Table, header.Key);

            if (header.Action == StorageActions.Store)
            {
                if (!index.ContainsKey(key))
                    index.Add(key, header);
                else
                    index[key] = header;

                stream.MovePositionForward(header.RecordSize);
            }
            else if (header.Action == StorageActions.Delete)
            {
                if (index.ContainsKey(key))
                    index.Remove(key);
            }
        }

        public byte[] Read(string table, string itemKey)
        {
            var key = new RecordKey(table, itemKey);

            if (_indexes.ContainsKey(key))
            {
                RecordHeader header = _indexes[key];

                return ReadFromStream(_dataStream, header);
            }

            return null;
        }

        byte[] ReadFromStream(Stream dataStream, RecordHeader header)
        {
            dataStream.SeekLocation(header.RecordLocation);
            return dataStream.Read(header.RecordSize);
        }

        public void CleanUp()
        {
            Stream tempStream = _tempStreamFactory();
            tempStream.WriteStreamHeader(Header);
            var newIndex = new Dictionary<RecordKey, RecordHeader>(new RecordKeyComparer());

            foreach (var kvp in _indexes)
            {
                byte[] data = ReadFromStream(_dataStream, kvp.Value);
                RecordHeader header = StoreToStream(tempStream, kvp.Value.Key, kvp.Value.Table, data,
                                                    StorageActions.Store);
                WriteHeaderToIndexForStream(header, newIndex, tempStream);
            }

            _primaryStreamCleanUp(_dataStream);
            _dataStream = _primaryStreamFactory();
            _dataStream.WriteStreamHeader(Header);

            foreach (var kvp in newIndex)
            {
                byte[] data = ReadFromStream(tempStream, kvp.Value);
                StoreToStream(_dataStream, kvp.Value.Key, kvp.Value.Table, data, StorageActions.Store);
            }

            _tempStreamCleanUp(tempStream);

            _indexes.Clear();

            _dataStream.SeekStart();

            ReadIndex();
        }
    }
}