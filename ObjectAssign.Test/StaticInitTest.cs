using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObjectAssign.Test
{

    [TestClass]
    public class StaticInitTest
    {
        [TestMethod]
        public void StaticInitTestMethod()
        {
            var test = new ClassA[0].AsQueryable();
            var resultA = test.Select(x => TestClass.cloneComp.Invoke(x));

            var list = resultA.ToList();
        }

    }

    public class TestClass
    {
        static Expression<Func<ClassA, ClassB>> clone = x => new ClassB
        {
            PropC = x.PropA
        };
        public static Expression<Func<ClassA, ClassB>> cloneComp = Tonic.LinqEx.CloneSimple(Tonic.LinqEx.CombineMemberInitExpression(clone));


    }
}
