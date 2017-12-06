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
    public class ImmutableTest
    {
        public class ImmutClass
        {
            public ImmutClass(string nombre, int edad, int id)
            {
                Nombre = nombre;
                Edad = edad;
                Id = id;
            }

            public string Nombre { get; }
            public int Edad { get; }
            public int Id { get; }
        }

        [TestMethod]
        public void TestSetImmut()
        {
            var val = new ImmutClass("Rafa", 20, 1);
            var newVal = LinqEx.SetImmutable(val, x => x.Edad, 23);

            //El valor original permanece igual
            Assert.AreEqual(20, val.Edad);

            //El nuevo elemento es una nueva referencia
            Assert.AreNotEqual(val, newVal);

            //Verificar propiedades
            Assert.AreEqual("Rafa", newVal.Nombre);
            Assert.AreEqual(1, newVal.Id);

            //Cambio la edad
            Assert.AreEqual(23, newVal.Edad);
        }
    }
}
