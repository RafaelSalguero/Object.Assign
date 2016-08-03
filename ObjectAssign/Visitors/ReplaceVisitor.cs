using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ObjectAssign.Visitors
{
    /// <summary>
    /// Replace a constant expression with another expression
    /// </summary>
    class ReplaceVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Create a constant replace expression visitor
        /// </summary>
        public ReplaceVisitor(Expression ex, Expression ReplaceWith)
        {
            this.Predicate = (e) => object.Equals(ex, e);
            this.Selector = (e) => ReplaceWith;
        }

        public ReplaceVisitor(Func<Expression, bool> Predicate, Func<Expression, Expression> Selector)
        {
            this.Predicate = Predicate;
            this.Selector = Selector;
        }

        Func<Expression, bool> Predicate;
        Func<Expression, Expression> Selector;

        public static Expression Replace(Expression Expression, Expression Find, Expression ReplaceWith)
        {
            var V = new ReplaceVisitor(Find, ReplaceWith);
            return V.Visit(Expression);
        }


        public bool Any = false;
        public override Expression Visit(Expression node)
        {
            if (Predicate(node))
            {
                Any = true;
                return Selector(node);
            }
            else
                return base.Visit(node);
        }
    }
}
