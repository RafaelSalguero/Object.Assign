using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tonic;

namespace ObjectAssign.Test
{
    public class ClassA
    {
        public string PropA { get; set; }
        public string PropB { get; set; }
        public string PropSame { get; set; }
    }
    public class ClassB : ClassA
    {
        public new int PropA { get; set; }
        public int PropB { get; set; }
        public string PropC { get; set; }
        public   int PropSame { get; set; }
    }
    public class ClassC : ClassB
    {
        public string Otra { get; set; }
    }

    [TestClass]
    public class InheritanceTest
    {
        [TestMethod]
        public void Inheritance()
        {
            var test = new ClassA[0].AsQueryable();
            var resultA = test.SelectCloneSimple(x => new ClassB
            {
            });

            var resultB = resultA.SelectCloneSimple(x => new ClassC
            {

            });

            List<ClassC> list = resultB.ToList();
        }

        [TestMethod]
        public void Inheritance2()
        {
            var test = new ClassA[] { new ClassA() }.AsQueryable();
            var resultA = test.SelectCloneSimple(x => new ClassB
            {
                PropA = 10
            });

            var list = resultA.ToList();
            Assert.AreEqual(10, list[0].PropA);
        }
        
        [TestMethod]
        public void Inheritance3()
        {
            var test = new ClassA[] { new ClassA() }.AsQueryable();
            var resultA = test.SelectCloneSimple(x => new ClassB
            {
                PropSame = 10
            });

            var list = resultA.ToList();
            Assert.AreEqual(10, list[0].PropSame);

            var resultB = test.AsEnumerable().SelectCloneSimple(x => new ClassB
            {
                PropSame = 30
            });

            list = resultB.ToList();
            Assert.AreEqual(30, list[0].PropSame);
        }
    }
}
