using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Wyhb.Joe.Cloning
{
    partial class ExpressionTreeCloner : ICloneFactory
    {
        #region CreateDeepPropertyBasedCloner

        /// <summary>Compiles a method that creates a deep clone of an object</summary>
        /// <param name="clonedType">Type for which a clone method will be created</param>
        /// <returns>A method that clones an object of the provided type</returns>
        /// <remarks>
        ///   <para>
        ///     The 'null' check is supposed to take place before running the cloner. This
        ///     avoids having redundant 'null' checks on nested types - first before calling
        ///     GetType() on the property to be cloned and second when runner the matching
        ///     cloner for the property.
        ///   </para>
        ///   <para>
        ///     This design also enables the cloning of nested value types (which can never
        ///     be null) without any null check whatsoever.
        ///   </para>
        /// </remarks>
        private static Func<object, object> CreateDeepPropertyBasedCloner(Type clonedType)
        {
            var original = Expression.Parameter(typeof(object), "original");

            var transferExpressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            if (clonedType.IsPrimitive || (clonedType == typeof(string)))
            {
                transferExpressions.Add(original);
            }
            else if (clonedType.IsArray)
            {
                var elementType = clonedType.GetElementType();

                if (elementType.IsPrimitive || (elementType == typeof(string)))
                {
                    transferExpressions.Add(GeneratePropertyBasedPrimitiveArrayTransferExpressions(clonedType, original, variables, transferExpressions));
                }
                else
                {
                    var typedOriginal = Expression.Variable(clonedType);
                    variables.Add(typedOriginal);
                    transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType)));

                    transferExpressions.Add(GeneratePropertyBasedComplexArrayTransferExpressions(clonedType, typedOriginal, variables, transferExpressions));
                }
            }
            else
            {
                var clone = Expression.Variable(clonedType);
                variables.Add(clone);

                transferExpressions.Add(Expression.Assign(clone, Expression.New(clonedType)));

                var typedOriginal = Expression.Variable(clonedType);
                variables.Add(typedOriginal); transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType)));

                GeneratePropertyBasedComplexTypeTransferExpressions(clonedType, typedOriginal, clone, variables, transferExpressions);

                transferExpressions.Add(clone);
            }

            Expression resultExpression;
            if ((transferExpressions.Count == 1) && (variables.Count == 0))
            {
                resultExpression = transferExpressions[0];
            }
            else
            {
                resultExpression = Expression.Block(variables, transferExpressions);
            }

            if (clonedType.IsValueType)
            {
                resultExpression = Expression.Convert(resultExpression, typeof(object));
            }

            return Expression.Lambda<Func<object, object>>(resultExpression, original).Compile();
        }

        #endregion CreateDeepPropertyBasedCloner

        #region CreateShallowPropertyBasedCloner

        /// <summary>Compiles a method that creates a deep clone of an object</summary>
        /// <param name="clonedType">Type for which a clone method will be created</param>
        /// <returns>A method that clones an object of the provided type</returns>
        /// <remarks>
        ///   <para>
        ///     The 'null' check is supposed to take place before running the cloner. This
        ///     avoids having redundant 'null' checks on nested types - first before calling
        ///     GetType() on the property to be cloned and second when runner the matching
        ///     cloner for the property.
        ///   </para>
        ///   <para>
        ///     This design also enables the cloning of nested value types (which can never
        ///     be null) without any null check whatsoever.
        ///   </para>
        /// </remarks>
        private static Func<object, object> CreateShallowPropertyBasedCloner(Type clonedType)
        {
            var original = Expression.Parameter(typeof(object), "original");

            var transferExpressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            if (clonedType.IsPrimitive || (clonedType == typeof(string)))
            {
                transferExpressions.Add(original);
            }
            else if (clonedType.IsArray)
            {
                transferExpressions.Add(GeneratePropertyBasedPrimitiveArrayTransferExpressions(clonedType, original, variables, transferExpressions));
            }
            else
            {
                var clone = Expression.Variable(clonedType);
                variables.Add(clone);
                transferExpressions.Add(Expression.Assign(clone, Expression.New(clonedType)));

                var typedOriginal = Expression.Variable(clonedType);
                variables.Add(typedOriginal);
                transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType)));

                GenerateShallowPropertyBasedComplexCloneExpressions(clonedType, typedOriginal, clone, transferExpressions, variables);

                transferExpressions.Add(clone);
            }

            Expression resultExpression;
            if ((transferExpressions.Count == 1) && (variables.Count == 0))
            {
                resultExpression = transferExpressions[0];
            }
            else
            {
                resultExpression = Expression.Block(variables, transferExpressions);
            }

            if (clonedType.IsValueType)
            {
                resultExpression = Expression.Convert(resultExpression, typeof(object));
            }

            return Expression.Lambda<Func<object, object>>(resultExpression, original).Compile();
        }

        #endregion CreateShallowPropertyBasedCloner

        #region GenerateShallowPropertyBasedComplexCloneExpressions

        /// <summary>
        ///   Generates expressions to transfer the properties of a complex value type
        /// </summary>
        /// <param name="clonedType">Complex value type that will be cloned</param>
        /// <param name="original">Original instance whose properties will be cloned</param>
        /// <param name="clone">Target instance into which the properties will be copied</param>
        /// <param name="transferExpressions">Receives the value transfer expressions</param>
        /// <param name="variables">Receives temporary variables used during the clone</param>
        private static void GenerateShallowPropertyBasedComplexCloneExpressions(Type clonedType, ParameterExpression original, ParameterExpression clone, ICollection<Expression> transferExpressions, ICollection<ParameterExpression> variables)
        {
            var propertyInfos = clonedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            propertyInfos.ToList().ForEach(p =>
            {
                if (p.CanRead && p.CanWrite)
                {
                    var propertyType = p.PropertyType;

                    if (propertyType.IsPrimitive || (propertyType == typeof(string)))
                    {
                        transferExpressions.Add(Expression.Assign(Expression.Property(clone, p), Expression.Property(original, p)));
                    }
                    else if (propertyType.IsValueType)
                    {
                        var originalProperty = Expression.Variable(propertyType);
                        variables.Add(originalProperty);
                        transferExpressions.Add(Expression.Assign(originalProperty, Expression.Property(original, p)));

                        var clonedProperty = Expression.Variable(propertyType);
                        variables.Add(clonedProperty);
                        transferExpressions.Add(Expression.Assign(clonedProperty, Expression.New(propertyType)));

                        GenerateShallowPropertyBasedComplexCloneExpressions(propertyType, originalProperty, clonedProperty, transferExpressions, variables);

                        transferExpressions.Add(Expression.Assign(Expression.Property(clone, p), clonedProperty));
                    }
                    else
                    {
                        transferExpressions.Add(Expression.Assign(Expression.Property(clone, p), Expression.Property(original, p)));
                    }
                }
            });
        }

        #endregion GenerateShallowPropertyBasedComplexCloneExpressions

        #region GeneratePropertyBasedPrimitiveArrayTransferExpressions

        /// <summary>
        ///   Generates state transfer expressions to copy an array of primitive types
        /// </summary>
        /// <param name="clonedType">Type of array that will be cloned</param>
        /// <param name="original">Variable expression for the original array</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        /// <returns>The variable holding the cloned array</returns>
        private static Expression GeneratePropertyBasedPrimitiveArrayTransferExpressions(Type clonedType, Expression original, ICollection<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var arrayCloneMethodInfo = typeof(Array).GetMethod("Clone");
            return Expression.Convert(Expression.Call(Expression.Convert(original, typeof(Array)), arrayCloneMethodInfo), clonedType);
        }

        #endregion GeneratePropertyBasedPrimitiveArrayTransferExpressions

        #region GeneratePropertyBasedComplexArrayTransferExpressions

        /// <summary>
        ///   Generates state transfer expressions to copy an array of complex types
        /// </summary>
        /// <param name="clonedType">Type of array that will be cloned</param>
        /// <param name="original">Variable expression for the original array</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        /// <returns>The variable holding the cloned array</returns>
        private static ParameterExpression GeneratePropertyBasedComplexArrayTransferExpressions(Type clonedType, Expression original, IList<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var clone = Expression.Variable(clonedType);
            variables.Add(clone);

            var dimensionCount = clonedType.GetArrayRank();
            var baseVariableIndex = variables.Count;
            var elementType = clonedType.GetElementType();

            var lengths = new List<ParameterExpression>();
            var indexes = new List<ParameterExpression>();
            var labels = new List<LabelTarget>();

            var arrayGetLengthMethodInfo = typeof(Array).GetMethod("GetLength");
            for (var idx = 0; idx < dimensionCount; ++idx)
            {
                lengths.Add(Expression.Variable(typeof(int)));
                variables.Add(lengths[idx]);
                transferExpressions.Add(Expression.Assign(lengths[idx], Expression.Call(original, arrayGetLengthMethodInfo, Expression.Constant(idx))));

                indexes.Add(Expression.Variable(typeof(int)));
                variables.Add(indexes[idx]);

                labels.Add(Expression.Label());
            }

            transferExpressions.Add(Expression.Assign(clone, Expression.NewArrayBounds(elementType, lengths)));

            transferExpressions.Add(Expression.Assign(indexes[0], Expression.Constant(0)));

            Expression innerLoop = null;
            for (var idx = dimensionCount - 1; idx >= 0; --idx)
            {
                var loopVariables = new List<ParameterExpression>();
                var loopExpressions = new List<Expression>();

                loopExpressions.Add(Expression.IfThen(Expression.GreaterThanOrEqual(indexes[idx], lengths[idx]), Expression.Break(labels[idx])));

                if (innerLoop == null)
                {
                    if (elementType.IsPrimitive || (elementType == typeof(string)))
                    {
                        loopExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), Expression.ArrayAccess(original, indexes)));
                    }
                    else if (elementType.IsValueType)
                    {
                        GeneratePropertyBasedComplexTypeTransferExpressions(elementType, Expression.ArrayAccess(original, indexes), Expression.ArrayAccess(clone, indexes), variables, loopExpressions);
                    }
                    else
                    {
                        var originalElement = Expression.Variable(elementType);
                        loopVariables.Add(originalElement);

                        loopExpressions.Add(Expression.Assign(originalElement, Expression.ArrayAccess(original, indexes)));

                        var nestedVariables = new List<ParameterExpression>();
                        var nestedTransferExpressions = new List<Expression>();

                        if (elementType.IsArray)
                        {
                            Expression clonedElement;

                            var nestedElementType = elementType.GetElementType();
                            if (nestedElementType.IsPrimitive || (nestedElementType == typeof(string)))
                            {
                                clonedElement = GeneratePropertyBasedPrimitiveArrayTransferExpressions(elementType, originalElement, nestedVariables, nestedTransferExpressions);
                            }
                            else
                            {
                                clonedElement = GeneratePropertyBasedComplexArrayTransferExpressions(elementType, originalElement, nestedVariables, nestedTransferExpressions);
                            }
                            nestedTransferExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), clonedElement));
                        }
                        else
                        {
                            var getOrCreateClonerMethodInfo = typeof(ExpressionTreeCloner).GetMethod("GetOrCreateDeepPropertyBasedCloner", BindingFlags.NonPublic | BindingFlags.Static);
                            var getTypeMethodInfo = typeof(object).GetMethod("GetType");
                            var invokeMethodInfo = typeof(Func<object, object>).GetMethod("Invoke");

                            nestedTransferExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), Expression.Convert(Expression.Call(Expression.Call(getOrCreateClonerMethodInfo, Expression.Call(originalElement, getTypeMethodInfo)), invokeMethodInfo, originalElement), elementType)));
                        }

                        loopExpressions.Add(Expression.IfThen(Expression.NotEqual(originalElement, Expression.Constant(null)), Expression.Block(nestedVariables, nestedTransferExpressions)));
                    }
                }
                else
                {
                    loopExpressions.Add(Expression.Assign(indexes[idx + 1], Expression.Constant(0)));
                    loopExpressions.Add(innerLoop);
                }

                loopExpressions.Add(Expression.PreIncrementAssign(indexes[idx]));

                innerLoop = Expression.Loop(Expression.Block(loopVariables, loopExpressions), labels[idx]);
            }

            transferExpressions.Add(innerLoop);

            return clone;
        }

        #endregion GeneratePropertyBasedComplexArrayTransferExpressions

        #region GeneratePropertyBasedComplexTypeTransferExpressions

        /// <summary>Generates state transfer expressions to copy a complex type</summary>
        /// <param name="clonedType">Complex type that will be cloned</param>
        /// <param name="original">Variable expression for the original instance</param>
        /// <param name="clone">Variable expression for the cloned instance</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        private static void GeneratePropertyBasedComplexTypeTransferExpressions(Type clonedType, Expression original, Expression clone, IList<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var propertyInfos = clonedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            propertyInfos.ToList().ForEach(p =>
            {
                if (p.CanRead && p.CanWrite)
                {
                    var propertyType = p.PropertyType;

                    if (propertyType.IsPrimitive || (propertyType == typeof(string)))
                    {
                        transferExpressions.Add(Expression.Assign(Expression.Property(clone, p), Expression.Property(original, p)));
                    }
                    else if (propertyType.IsValueType)
                    {
                        ParameterExpression originalProperty = Expression.Variable(propertyType);
                        variables.Add(originalProperty);
                        ParameterExpression clonedProperty = Expression.Variable(propertyType);
                        variables.Add(clonedProperty);

                        transferExpressions.Add(Expression.Assign(originalProperty, Expression.Property(original, p)));
                        transferExpressions.Add(Expression.Assign(clonedProperty, Expression.New(propertyType)));

                        GeneratePropertyBasedComplexTypeTransferExpressions(propertyType, originalProperty, clonedProperty, variables, transferExpressions);

                        transferExpressions.Add(Expression.Assign(Expression.Property(clone, p), clonedProperty));
                    }
                    else
                    {
                        GeneratePropertyBasedReferenceTypeTransferExpressions(original, clone, transferExpressions, variables, p, propertyType);
                    }
                }
            });
        }

        #endregion GeneratePropertyBasedComplexTypeTransferExpressions

        #region GeneratePropertyBasedReferenceTypeTransferExpressions

        /// <summary>
        ///   Generates the expressions to transfer a reference type (array or class)
        /// </summary>
        /// <param name="original">Original value that will be cloned</param>
        /// <param name="clone">Variable that will receive the cloned value</param>
        /// <param name="transferExpressions">
        ///   Receives the expression generated to transfer the values
        /// </param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="propertyInfo">Reflection informations about the property being cloned</param>
        /// <param name="propertyType">Type of the property being cloned</param>
        private static void GeneratePropertyBasedReferenceTypeTransferExpressions(Expression original, Expression clone, ICollection<Expression> transferExpressions, ICollection<ParameterExpression> variables, PropertyInfo propertyInfo, Type propertyType)
        {
            var originalProperty = Expression.Variable(propertyType);
            variables.Add(originalProperty);

            transferExpressions.Add(Expression.Assign(originalProperty, Expression.Property(original, propertyInfo)));

            var propertyTransferExpressions = new List<Expression>();
            var propertyVariables = new List<ParameterExpression>();

            if (propertyType.IsArray)
            {
                Expression propertyClone;

                var elementType = propertyType.GetElementType();
                if (elementType.IsPrimitive || (elementType == typeof(string)))
                {
                    propertyClone = GeneratePropertyBasedPrimitiveArrayTransferExpressions(propertyType, originalProperty, propertyVariables, propertyTransferExpressions);
                }
                else
                {
                    propertyClone = GeneratePropertyBasedComplexArrayTransferExpressions(propertyType, originalProperty, propertyVariables, propertyTransferExpressions);
                }

                propertyTransferExpressions.Add(Expression.Assign(Expression.Property(clone, propertyInfo), propertyClone));
            }
            else
            {
                var getOrCreateClonerMethodInfo = typeof(ExpressionTreeCloner).GetMethod("GetOrCreateDeepPropertyBasedCloner", BindingFlags.NonPublic | BindingFlags.Static);
                var getTypeMethodInfo = typeof(object).GetMethod("GetType");
                var invokeMethodInfo = typeof(Func<object, object>).GetMethod("Invoke");

                propertyTransferExpressions.Add(Expression.Assign(Expression.Property(clone, propertyInfo), Expression.Convert(Expression.Call(Expression.Call(getOrCreateClonerMethodInfo, Expression.Call(originalProperty, getTypeMethodInfo)), invokeMethodInfo, originalProperty), propertyType)));
            }

            transferExpressions.Add(Expression.IfThen(Expression.NotEqual(originalProperty, Expression.Constant(null)), Expression.Block(propertyVariables, propertyTransferExpressions)));
        }

        #endregion GeneratePropertyBasedReferenceTypeTransferExpressions
    }
}