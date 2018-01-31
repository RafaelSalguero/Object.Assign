using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        public void CombineMemberInitTest()
        {
            Expression<Func<Client, ClientDTO>> a = x => new ClientDTO
            {
                Name = "Hello " + x.Name,
                Age = x.Age
            };

            Expression<Func<Client, ClientDTO>> b = x => new ClientDTO
            {
                LegalDrinking = true,
                Age = x.Age + 1
            };

            var comb = LinqEx.CombineMemberInitExpression(a, b);
            var res = comb.Compile()(new Client
            {
                Name = "Rafael",
                Age = 20,
            });

            Assert.AreEqual("Hello Rafael", res.Name);
            Assert.AreEqual(21, res.Age);
            Assert.AreEqual(true, res.LegalDrinking);
        }

        [TestMethod]
        public void CombineMemberInitTestMultiParam()
        {
            Expression<Func<Client, int, ClientDTO>> a = (x, y) => new ClientDTO
            {
                Name = "Hello " + x.Name + " " + y,
                Age = x.Age
            };

            Expression<Func<Client, int, ClientDTO>> b = (x, y) => new ClientDTO
            {
                LegalDrinking = true,
                Age = x.Age + y
            };

            var comb = LinqEx.CombineMemberInitExpression(a, b);
            var res = comb.Compile()(new Client
            {
                Name = "Rafael",
                Age = 20,
            }, 3);

            Assert.AreEqual("Hello Rafael 3", res.Name);
            Assert.AreEqual(23, res.Age);
            Assert.AreEqual(true, res.LegalDrinking);
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

        [TestMethod]
        public void InMemorySelectClone()
        {
            var Clients = new Client[]
        {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Subclient { Name = "Jose", Age = 17 } },

                };

            var Ret1 = Clients.SelectClone(x => new ClientDTO { }).First();
            Assert.AreEqual(Clients.First().Name, Ret1.Name);
            Assert.AreEqual(Clients.First().Age, Ret1.Age);
            Assert.AreEqual(Clients.First().NotSimple, Ret1.NotSimple);

        }


        [TestMethod]
        public void InMemorySelectCloneSimple()
        {
            var Clients = new Client[]
        {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Subclient { Name = "Jose", Age = 17 } },

                };

            var Ret1 = Clients.SelectCloneSimple(x => new ClientDTO { }).First();
            Assert.AreEqual(Clients.First().Name, Ret1.Name);
            Assert.AreEqual(Clients.First().Age, Ret1.Age);
            Assert.AreEqual(null, Ret1.NotSimple);

        }

        [TestMethod]
        public void InMemorySelectCloneMemberInit()
        {
            var Clients = new Client[]
        {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Subclient { Name = "Jose", Age = 17 } },

                };

            var Ret1 = Clients.SelectClone(x => new ClientDTO { Age = x.Age + 2 }).First();
            Assert.AreEqual(Clients.First().Name, Ret1.Name);
            Assert.AreEqual(24, Ret1.Age);
            Assert.AreEqual(Clients.First().NotSimple, Ret1.NotSimple);
        }

        [TestMethod]
        public void InMemorySelectCloneSimpleMemberInit()
        {
            var Clients = new Client[]
        {
                new Client {
                    Name = "Rafael",
                    Age = 22,
                    NotSimple = new Subclient { Name = "Jose", Age = 17 } },

                };

            var Ret1 = Clients.SelectCloneSimple(x => new ClientDTO { Age = 20 }).First();

            Ret1 = Clients.SelectCloneSimple(x => new ClientDTO { Age = 20 }).First();
            Assert.AreEqual(Clients.First().Name, Ret1.Name);
            Assert.AreEqual(20, Ret1.Age);
            Assert.AreEqual(null, Ret1.NotSimple);

        }

    }
}
