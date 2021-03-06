﻿using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObjectAssign.Test
{
    public class A
    {
    }

    public class B : A
    {

    }


    public class Persona
    {
        public int Edad { get; set; }
        public string Nombre { get; set; }

        public ICollection<A> AList { get; set; }
    }

    public class PersonaDTO : Persona
    {
        public new int Edad { get; set; }
        public new IReadOnlyList<B> AList { get; set; }
    }

    public class OtroDTO : PersonaDTO
    {
        public new string Edad { get; set; }
    }

    [TestClass]
    public class MemberShadowingTest
    {
        [TestMethod]
        public void SimpleClone()
        {
          var expr =   Tonic.LinqEx.CloneSimple<Persona, PersonaDTO>(x => new PersonaDTO
            {
            });

            var a = new Persona
            {
                Edad = 21,
                Nombre = "rafa"
            };

            var b = expr.Compile().Invoke(a);

            Assert.AreEqual(21, b.Edad);
            Assert.AreEqual("rafa", b.Nombre);
        }

        [TestMethod]
        public void SimpleSubstitution ()
        {
            var expr = Tonic.LinqEx.CloneSimple<Persona, PersonaDTO>(x => new PersonaDTO
            {
                Edad = 10,
            });

            var a = new Persona
            {
                Edad = 21,
                Nombre = "rafa"
            };

            var b = expr.Compile().Invoke(a);

            Assert.AreEqual(10, b.Edad);
            Assert.AreEqual("rafa", b.Nombre);
        }

        [TestMethod]
        public void SimpleSubstitutionMultiParam()
        {
            var expr = Tonic.LinqEx.CloneSimple<Persona, int, PersonaDTO>((x, nuevo) => new PersonaDTO
            {
                Edad = nuevo + 3,
            });

            var a = new Persona
            {
                Edad = 21,
                Nombre = "rafa"
            };

            var b = expr.Compile().Invoke(a, 35);

            Assert.AreEqual(38, b.Edad);
            Assert.AreEqual("rafa", b.Nombre);

            var c = expr.Compile().Invoke(a, 25);

            Assert.AreEqual(28, c.Edad);
            Assert.AreEqual("rafa", c.Nombre);
        }

        [TestMethod]
        public void TypeSubstitutionClone()
        {
            var expr = Tonic.LinqEx.CloneSimple<Persona, OtroDTO>(x => new OtroDTO
            {
            });

            var a = new Persona
            {
                Edad = 21,
                Nombre = "rafa"
            };

            var b = expr.Compile().Invoke(a);

            Assert.AreEqual(null, b.Edad);
            Assert.AreEqual("rafa", b.Nombre);
        }

        [TestMethod]
        public void TypeSubstitution()
        {
            var expr = Tonic.LinqEx.CloneSimple<Persona, OtroDTO>(x => new OtroDTO
            {
                 Edad = "La edad es: " + x.Edad
            });

            var a = new Persona
            {
                Edad = 21,
                Nombre = "rafa"
            };

            var b = expr.Compile().Invoke(a);

            Assert.AreEqual("La edad es: 21", b.Edad);
            Assert.AreEqual("rafa", b.Nombre);
        }
    }
}
