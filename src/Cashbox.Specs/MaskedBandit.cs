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
    using System.IO;
    using System.Linq;
    using Magnum.TestFramework;
    using NUnit.Framework;


    [TestFixture]
    public class MaskedBandit
    {
        [Test]
        public void Robbin_the_bank()
        {
            using (IDocumentSession session = DocumentSessionFactory.Create(InsertStoreName))
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

            using (IDocumentSession session = DocumentSessionFactory.Create(InsertStoreName))
            {
                var sw = new Stopwatch();
                sw.Start();
                for (int i = 0; i < EventCount; i++)
                {
                    var document = session.Retrieve<NumericDocument>(i.ToString())
                        ;
                    int result = document.Number;

                    Assert.That(result, Is.EqualTo(i));
                }

                sw.Stop();

                Console.WriteLine("10k reads: {0}ms", sw.ElapsedMilliseconds);
            }

            using (IDocumentSession session = DocumentSessionFactory.Create(InsertStoreName))
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
                // we are going to assume that there will no more than a 1/3 of the delets as collisions
                count.ShouldBeLessThan(EventCount - (EventCount/100/3));
            }
        }

        const string InsertStoreName = "10k_insert.store";

        [TestFixtureSetUp]
        public void CleanUpExistingFiles()
        {
            if (File.Exists(InsertStoreName))
                File.Delete(InsertStoreName);
        }

        const int EventCount = 10000;
    }
}