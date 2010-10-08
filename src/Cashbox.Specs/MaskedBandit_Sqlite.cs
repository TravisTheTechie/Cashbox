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
    using Implementations;
    using NUnit.Framework;


    [TestFixture]
    public class MaskedBandit_Sqlite :
        MaskedBanditBase
    {
        [Test]
        public void Robbin_the_sqlite_bank()
        {
            DocumentSessionFactory.SetEngineFactory(str => new SqliteEngine(str));

            RobTheBank(InsertStoreName);
        }

        const string InsertStoreName = "10k_insert.sqlite.store";

        [TestFixtureSetUp]
        public void CleanUpExistingFiles()
        {
            //if (File.Exists(InsertStoreName))
            //    File.Delete(InsertStoreName);
        }
    }
}