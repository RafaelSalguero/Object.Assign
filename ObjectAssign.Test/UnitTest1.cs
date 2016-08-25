using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tonic;

namespace ObjectAssign.Test
{
    [TestClass]
    public class UnitTest1
    {
        class Client
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string InternalData { get; set; }
        }

        class ClientDTO
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool LegalDrinking { get; set; }
        }

        [TestMethod]
        public void MapTypeTest()
        {
            var Map = LinqEx.MapTypes(typeof(Client), typeof(ClientDTO));

            Assert.AreEqual(2, Map.Count);

            Assert.AreEqual(typeof(Client).GetProperty("Name"), Map["Name"].Source);
            Assert.AreEqual(typeof(Client).GetProperty("Age"), Map["Age"].Source);

            Assert.AreEqual(typeof(ClientDTO).GetProperty("Name"), Map["Name"].Dest);
            Assert.AreEqual(typeof(ClientDTO).GetProperty("Age"), Map["Age"].Dest);
        }

        [TestMethod]
        public void ExpressionBindingTest()
        {
            var Extract = LinqEx.ExtractBindings<Client, ClientDTO>(x => new ClientDTO
            {
                Age = x.Age + 20,
                LegalDrinking = x.Name == "Rafa"
            }, Expression.Parameter(typeof(Client), "hello"));

            Assert.AreEqual("(hello.Age + 20)", Extract["Age"].Expression.ToString());
            Assert.AreEqual("(hello.Name == \"Rafa\")", Extract["LegalDrinking"].Expression.ToString());
        }

        [TestMethod]
        public void PopulateObjectTest()
        {
            var Source = new Client { Name = "Rafael", Age = 22 };
            var Dest = new ClientDTO();

            LinqEx.PopulateObject(Source, Dest, 
                new Dictionary<string, object> {
                    { "Age", 20 },
                    {"LegalDrinking", true }
                }, 
                x => true);

            Assert.AreEqual("Rafael", Dest.Name);
            Assert.AreEqual(20, Dest.Age);
            Assert.AreEqual(true, Dest.LegalDrinking);
        }

        [TestMethod]
        public void MemberInitTestImmediate()
        {
            var Clients = new Client[]
            {
                new Client { Name = "Rafael", Age = 22 },
                new Client { Name = "Jose", Age = 17 },
            }.AsQueryable();

            //Syntax test:
            Clients.Select(LinqEx.Clone<Client, ClientDTO>(x => new ClientDTO { Age = 20 }));

            Clients.SelectClone(x => new ClientDTO { Age = 20 });

            var Ret = LinqEx.Clone<Client, ClientDTO>().Compile().Invoke(Clients.First());

            Assert.AreEqual(22, Ret.Age);
            Assert.AreEqual("Rafael", Ret.Name);
            Assert.AreEqual(false, Ret.LegalDrinking);

            Ret = LinqEx.Clone<Client, ClientDTO>(x => new ClientDTO { LegalDrinking = x.Age > 18 }).Compile().Invoke(Clients.First());

            Assert.AreEqual(22, Ret.Age);
            Assert.AreEqual("Rafael", Ret.Name);
            Assert.AreEqual(true, Ret.LegalDrinking);

            var Expr = LinqEx.Clone<Client, ClientDTO>(x => new ClientDTO { LegalDrinking = x.Age > 18, Name = "Luis" });
            Ret = Expr.Compile().Invoke(Clients.First());

            Assert.AreEqual(22, Ret.Age);
            Assert.AreEqual("Luis", Ret.Name);
            Assert.AreEqual(true, Ret.LegalDrinking);
        }
    }
}
