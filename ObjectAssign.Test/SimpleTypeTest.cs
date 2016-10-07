using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tonic;

namespace ObjectAssign.Test
{
    [TestClass]
    public class SimpleTypeTest
    {
        class Client
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string InternalData { get; set; }
            public Subclient NotSimple { get; set; }
            public Client NotSimple2 { get; set; }
            public int AgePlusOne => Age + 1;
        }

        class Subclient : Client
        {
            public string Phone { get; set; }
        }

        class ClientDTO
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool LegalDrinking { get; set; }
            public Client NotSimple { get; set; }
            public Client NotSimple2 { get; set; }

            public int AgePlusOne => Age + 1;
        }

        [TestMethod]
        public void PopulateObject()
        {
            var A = new Client { Age = 21, Name = "Rafael", InternalData = "Hello", NotSimple = null };
            var B = new ClientDTO();

            LinqEx.PopulateObject(A, B);

        }

        [TestMethod]
        public void OnlySimpleTest()
        {
            var Clients = new Client[]
          {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Subclient { Name = "Jose", Age = 17 } },

          }.AsQueryable();


            var Ret1 = Clients.SelectClone(x => new ClientDTO { Age = 20 }).First();
            Assert.AreEqual(Clients.First().NotSimple, Ret1.NotSimple);
            Assert.AreEqual(Clients.First().NotSimple2, Ret1.NotSimple2);


            var Ret2 = Clients.SelectCloneSimple(x => new ClientDTO { Age = 20 }).First();
            Assert.IsNull(Ret2.NotSimple);
            Assert.IsNull(Ret2.NotSimple2);
        }
    }
}
