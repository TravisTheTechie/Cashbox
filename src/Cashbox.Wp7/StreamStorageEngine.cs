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
namespace Cashbox.Wp7
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Engines.FileStorage;


    public class StreamStorageEngine :
        IDisposable
    {
        readonly StreamStorage _storage;
        int _changeCounter;


        public StreamStorageEngine(Func<Stream> primaryStreamFactory, Func<Stream> tempStreamFactory)
        {
            CompactionFrequency = 250;
            _storage = new StreamStorage(primaryStreamFactory, tempStreamFactory, s => s.Close(), s => s.Close());
        }

        protected int CompactionFrequency { get; set; }

        public void Dispose()
        {
            _storage.Dispose();
        }

        void IncrementChangeCounter()
        {
            _changeCounter++;
            if (_changeCounter == CompactionFrequency)
                _storage.CleanUp();
        }

        public T RetrieveValue<T>(string key)
            where T : class
        {
            byte[] data = _storage.Read(typeof(T).ToString(), key);
            return MagicJsonSeralizer.Deserialize<T>(data);
        }

        public void Store<T>(string key, T value)
            where T : class
        {
            byte[] data = MagicJsonSeralizer.Serialize(value);
            _storage.Store(typeof(T).ToString(), key, data);
            IncrementChangeCounter();
        }

        public IEnumerable<T> List<T>()
            where T : class
        {
            return _storage
                .Keys
                .Where(x => x.Table == typeof(T).ToString())
                .Select(x => RetrieveValue<T>(x.Key))
                .AsEnumerable();
        }
    }
}