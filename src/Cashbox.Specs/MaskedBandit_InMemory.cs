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
	using System.IO;
	using Engines;
	using NUnit.Framework;


	[TestFixture]
	public class MaskedBandit_InMemory :
		MaskedBanditBase
	{
		[Test]
		public void Robbin_the_in_memory_bank()
		{
			DocumentSessionFactory.SetEngineFactory(str => new InMemoryEngine(str));

			RobTheBank(InsertStoreName);
		}

		const string InsertStoreName = "10k_insert.inmemory.store";

		[TestFixtureSetUp]
		public void CleanUpExistingFiles()
		{
			if (File.Exists(InsertStoreName))
				File.Delete(InsertStoreName);
		}
	}
}