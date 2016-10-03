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
            public Client NotSimple { get; set; }
        }

        class ClientDTO
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool LegalDrinking { get; set; }
            public Client NotSimple { get; set; }

        }

        [TestMethod]
        public void OnlySimpleTest()
        {
            var Clients = new Client[]
          {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Client { Name = "Jose", Age = 17 } },

          }.AsQueryable();


            var Ret1 = Clients.SelectClone(x => new ClientDTO { Age = 20 }).First();
            Assert.AreEqual( Clients.First().NotSimple, Ret1.NotSimple);


            var Ret2 = Clients.SelectCloneSimple(x => new ClientDTO { Age = 20 }).First();
            Assert.IsNull(Ret2.NotSimple);
        }
    }
}
