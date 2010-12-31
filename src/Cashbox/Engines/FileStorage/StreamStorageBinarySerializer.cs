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


	public class StreamStorageBinarySerializer
	{
		public static byte[] SerializeRecordHeader(RecordHeader header)
		{
			var results = new List<byte>();

			results.AddRange(BitConverter.GetBytes(header.HeaderVersion));
			results.AddRange(BitConverter.GetBytes(header.RecordSize));
			results.AddRange(BitConverter.GetBytes((int)header.Action));

			return results.ToArray();
		}

		public static byte[] SerializeStreamHeader(StreamHeader header)
		{
			var results = new List<byte>();

			results.AddRange(BitConverter.GetBytes(header.Version));

			return results.ToArray();
		}

		public static RecordHeader DeserializerRecordHeader(Stream dataStream)
		{
			var br = new BinaryReader(dataStream);

			var result = new RecordHeader
				{
					HeaderVersion = br.ReadInt32(),
					RecordSize = br.ReadInt32(),
					Action = (StorageActions)br.ReadInt32()
				};

			return result;
		}

		public static StreamHeader DeserializerStreamHeader(Stream dataStream)
		{
			var br = new BinaryReader(dataStream);

			var result = new StreamHeader
				{
					Version = br.ReadInt32()
				};

			return result;
		}
	}
}