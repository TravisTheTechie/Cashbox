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


	public static class StreamExtensions
	{
		public static void SeekStart(this Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);
		}

		public static void SeekEnd(this Stream stream)
		{
			stream.Seek(0, SeekOrigin.End);
		}

		public static void SeekLocation(this Stream stream, long position)
		{
			stream.Seek(position, SeekOrigin.Begin);
		}

		public static void MovePositionForward(this Stream stream, long offset)
		{
			stream.Seek(offset, SeekOrigin.Current);
		}

		public static void Write(this Stream stream, byte[] data)
		{
			if (data == null || data.Length == 0)
				return;

			stream.Write(data, 0, data.Length);
		}
	}
}