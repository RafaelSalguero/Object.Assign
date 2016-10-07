using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tonic;

namespace ObjectAssign.Test
{
    [TestClass]
    public class ComplexTypeTest
    {
        [ComplexType]
        class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
        }
        class Client
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string InternalData { get; set; }
            public Client NotSimple { get; set; }
            public Address Address { get; set; }
        }

        class ClientDTO
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool LegalDrinking { get; set; }
            public Client NotSimple { get; set; }
            public Address Address { get; set; }
        }

        [TestMethod]
        public void ComplexTypePopulateSimpleTest()
        {

            var Source = new Client
            {
                Name = "Rafael",
                Age = 22,
                NotSimple = new Client { Name = "Jose", Age = 17 },
                Address = new Address
                {
                    City = "Obregon",
                    Street = "E Baca Calderon"
                }
            };

            var Dest = new ClientDTO();

            LinqEx.PopulateObjectSimple(Source, Dest);

            Assert.AreEqual(Dest.Name, Source.Name);
            Assert.AreEqual(Dest.Age, Source.Age);
            Assert.IsNull(Dest.NotSimple);

            Assert.IsNotNull(Dest.Address);
        
            //Deep cloning should result in different instances
            Assert.AreNotEqual(Source.Address, Dest.Address);
            Assert.AreEqual(Source.Address.City, Dest.Address.City);
            Assert.AreEqual(Source.Address.Street, Dest.Address.Street);
        }

        [TestMethod]
        public void ComplexTypeExpressionTest()
        {
            var ex = Tonic.LinqEx.CloneSimple<Client, ClientDTO>();
            Expression<Func<Client, ClientDTO>> desiredExpression = x => new ClientDTO
            {
                Name = x.Name,
                Age = x.Age,
                Address = new Address
                {
                    Street = x.Address.Street,
                    City = x.Address.City,
                }
            };
            Assert.AreEqual(ex.ToString(), desiredExpression.ToString());
        }

        [TestMethod]
        public void ComplexTypeTestMethod()
        {
            var Clients = new Client[]
            {
                    new Client {
                        Name = "Rafael",
                        Age = 22,
                        NotSimple = new Client { Name = "Jose", Age = 17 },
                        Address = new Address {
                                City ="Obregon",
                                 Street ="E Baca Calderon"
                        }
                    },
            }.AsQueryable();

            var ret = Clients.SelectCloneSimple(x => new ClientDTO { });

            var Original = Clients.First();
            var Result = ret.First();

            Assert.AreEqual(Original.Name, Result.Name);
            Assert.AreEqual(Original.Age, Result.Age);

            //Result.Address is not the same instance as Original.Address since deep clone was used
            Assert.AreNotEqual(Original.Address, Result.Address);

            Assert.AreEqual(Original.Address.City, Result.Address.City);
            Assert.AreEqual(Original.Address.Street, Result.Address.Street);
        }
    }
}
