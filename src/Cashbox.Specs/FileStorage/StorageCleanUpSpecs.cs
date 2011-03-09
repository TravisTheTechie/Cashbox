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
namespace Cashbox.Specs.FileStorage
{
	using System;
	using System.IO;
	using Engines.FileStorage;
	using Magnum.TestFramework;


	public class StreamStorageCleanUpSpecsBase
	{
		protected bool HasCalledTempStreamFactory { get; set; }
		protected string Table { get; set; }
		protected StreamStorage Storage { get; set; }


		[Given]
		public void A_stream_storage_instance_with_stream_factories()
		{
			Func<Stream> primaryStreamFactory = () => new MemoryStream();
			Func<Stream> tempStreamFactory = () =>
				{
					HasCalledTempStreamFactory = true;
					return new MemoryStream();
				};
			Action<Stream> primaryStreamCleanUp = s => s.Close();
			Action<Stream> tempStreamCleanUp = s => s.Close();

			Storage = new StreamStorage(primaryStreamFactory,
			                            tempStreamFactory,
			                            primaryStreamCleanUp,
			                            tempStreamCleanUp);

			Table = "Test";

			for (int i = 0; i < 110; i++)
				Storage.Store(Table, i.ToString(), BitConverter.GetBytes(i));

			for (int i = 11; i < 33; i++)
				Storage.Remove(Table, i.ToString());

			Storage.CleanUp();
		}
	}


	[Scenario]
	public class When_over_100_transactions_are_hit :
		StreamStorageCleanUpSpecsBase
	{
		[Then]
		public void Temp_stream_factory_should_have_been_called()
		{
			HasCalledTempStreamFactory.ShouldBeTrue();
		}
	}
}