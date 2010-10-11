// Copyright 2010 Travis Smith
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
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
                for (int i = 0; i < EventCount / 100; i++)
                {
                    string key = rand.Next(EventCount - 1).ToString();
                    session.Delete<NumericDocument>(key);
                }
                sw.Stop();
                int count = session.List<NumericDocument>().Count();
                Console.WriteLine("{1} (of {2} attempted) deletes: {0}ms", sw.ElapsedMilliseconds, EventCount - count,
                                  EventCount / 100);
                // at least one delete should have happened
                count.ShouldBeLessThan(EventCount - 1);
            }
            swFull.Stop();
            Console.WriteLine("Spin up, delete, count, and shutdown: {0}ms", swFull.ElapsedMilliseconds);
        }
    }
}