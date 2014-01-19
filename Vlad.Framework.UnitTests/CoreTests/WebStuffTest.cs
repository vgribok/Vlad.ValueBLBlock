﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;

using Example.AdventureWorks2008ObjectContext_Dal;

namespace Aspectacular.Test
{
    /// <summary>
    /// Summary description for WebStuffTest
    /// </summary>
    [TestClass]
    public class WebStuffTest
    {
        public WebStuffTest()
        {
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in addr class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestJSonSerializtion()
        {
            string nullObj = null;
            string json = nullObj.ToJsonString();
            Assert.AreEqual("null", json);

            json.ToString();
            nullObj = json.FromJsonString<string>();
            Assert.AreEqual(null, nullObj);

            IList<Address> addresses = LinqTests.GetQueryCustomerAddressesByCustomerID();
            json = AOP.Invoke(AspectacularTest.TestAspects, () => addresses.ToJsonString());

            Address[] deserializedAddresses = json.FromJsonString<Address[]>();
            
            Assert.AreEqual(addresses.Count, deserializedAddresses.Length);
            deserializedAddresses.For((addr, i) => Assert.AreEqual(addr[i].ToJsonString(), deserializedAddresses[i].ToJsonString()));

            deserializedAddresses[0].AddressID ++;
            Assert.AreNotEqual(addresses[0].ToJsonString(), deserializedAddresses[0].ToJsonString());
        }
    }
}
