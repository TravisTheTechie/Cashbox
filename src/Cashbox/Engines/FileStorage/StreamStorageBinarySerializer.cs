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
	using System.IO;


	public class StreamStorageBinarySerializer
	{
		public static void SerializeRecordHeader(Stream dataStream, RecordHeader header)
		{
			var bw = new BinaryWriter(dataStream);

			bw.Write(header.HeaderVersion);
			bw.Write(header.RecordSize);
			bw.Write((int)header.Action);
			bw.Write(header.Key);
		}

		public static RecordHeader DeserializeRecordHeader(Stream dataStream)
		{
			var br = new BinaryReader(dataStream);

			var result = new RecordHeader
				{
					HeaderVersion = br.ReadInt32(),
					RecordSize = br.ReadInt64(),
					Action = (StorageActions)br.ReadInt32(),
					Key = br.ReadString()
				};

			return result;
		}

		public static void SerializeStreamHeader(Stream dataStream, StreamHeader header)
		{
			var bw = new BinaryWriter(dataStream);

			bw.Write(header.Version);
		}

		public static StreamHeader DeserializeStreamHeader(Stream dataStream)
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