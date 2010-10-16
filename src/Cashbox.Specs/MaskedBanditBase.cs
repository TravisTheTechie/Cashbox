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
namespace Cashbox.Specs
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using Magnum.Extensions;
	using Magnum.TestFramework;
	using NUnit.Framework;


	public class MaskedBanditBase
	{
		const int EventCount = 10000;

		public void RobTheBank(string storeFilename)
		{
			var swFull = new Stopwatch();
			swFull.Start();
			using (DocumentSession session = DocumentSessionFactory.Create(storeFilename))
			{
				var sw = new Stopwatch();
				sw.Start();
				for (int i = 0; i < EventCount; i++)
				{
					session.Store(i.ToString(), new NumericDocument
						{
							Number = i
						});
				}
				sw.Stop();

				Console.WriteLine("10k inserts: {0}ms", sw.ElapsedMilliseconds);
			}
			swFull.Stop();
			Console.WriteLine("Spin up, insert, and shutdown: {0}ms", swFull.ElapsedMilliseconds);

			swFull.Reset();
			swFull.Start();
			using (DocumentSession session = DocumentSessionFactory.Create(storeFilename))
			{
				var sw = new Stopwatch();
				sw.Start();
				for (int i = 0; i < EventCount; i++)
				{
					var document = session.Retrieve<NumericDocument>(i.ToString());
					Assert.That(document, Is.Not.Null, "Document not found for record {0}".FormatWith(i));
					int result = document.Number;

					Assert.That(result, Is.EqualTo(i));
				}

				sw.Stop();

				Console.WriteLine("10k reads: {0}ms", sw.ElapsedMilliseconds);
			}
			swFull.Stop();
			Console.WriteLine("Spin up, assert each, and shutdown: {0}ms", swFull.ElapsedMilliseconds);

			swFull.Reset();
			swFull.Start();
			using (DocumentSession session = DocumentSessionFactory.Create(storeFilename))
			{
				var rand = new Random();

				var sw = new Stopwatch();
				sw.Start();
				for (int i = 0; i < EventCount/100; i++)
				{
					string key = rand.Next(EventCount - 1).ToString();
					session.Delete<NumericDocument>(key);
				}
				sw.Stop();
				int count = session.List<NumericDocument>().Count();
				Console.WriteLine("{1} (of {2} attempted) deletes: {0}ms", sw.ElapsedMilliseconds, EventCount - count,
				                  EventCount/100);
				// at least one delete should have happened
				count.ShouldBeLessThan(EventCount - 1);
			}
			swFull.Stop();
			Console.WriteLine("Spin up, delete, count, and shutdown: {0}ms", swFull.ElapsedMilliseconds);
		}
	}
}