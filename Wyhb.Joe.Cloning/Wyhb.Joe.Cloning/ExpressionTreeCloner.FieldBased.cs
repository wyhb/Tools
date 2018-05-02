using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Wyhb.Joe.Cloning
{
    partial class ExpressionTreeCloner : ICloneFactory
    {
        #region CreateDeepFieldBasedCloner

        /// <summary>Compiles a method that creates a deep clone of an object</summary>
        /// <param name="clonedType">Type for which a clone method will be created</param>
        /// <returns>A method that clones an object of the provided type</returns>
        /// <remarks>
        ///   <para>
        ///     The 'null' check is supposed to take place before running the cloner. This
        ///     avoids having redundant 'null' checks on nested types - first before calling
        ///     GetType() on the field to be cloned and second when runner the matching
        ///     cloner for the field.
        ///   </para>
        ///   <para>
        ///     This design also enables the cloning of nested value types (which can never
        ///     be null) without any null check whatsoever.
        ///   </para>
        /// </remarks>
        private static Func<object, object> CreateDeepFieldBasedCloner(Type clonedType)
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
                    transferExpressions.Add(GenerateFieldBasedPrimitiveArrayTransferExpressions(clonedType, original, variables, transferExpressions));
                }
                else
                {
                    var typedOriginal = Expression.Variable(clonedType);
                    variables.Add(typedOriginal);
                    transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType)));
                    transferExpressions.Add(GenerateFieldBasedComplexArrayTransferExpressions(clonedType, typedOriginal, variables, transferExpressions));
                }
            }
            else
            {
                var clone = Expression.Variable(clonedType);
                variables.Add(clone);
                var getUninitializedObjectMethodInfo = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Static | BindingFlags.Public);
                transferExpressions.Add(Expression.Assign(clone, Expression.Convert(Expression.Call(getUninitializedObjectMethodInfo, Expression.Constant(clonedType)), clonedType)));
                var typedOriginal = Expression.Variable(clonedType);
                variables.Add(typedOriginal);
                transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType)));
                GenerateFieldBasedComplexTypeTransferExpressions(clonedType, typedOriginal, clone, variables, transferExpressions);
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

        #endregion CreateDeepFieldBasedCloner

        #region CreateShallowFieldBasedCloner

        /// <summary>Compiles a method that creates a shallow clone of an object</summary>
        /// <param name="clonedType">Type for which a clone method will be created</param>
        /// <returns>A method that clones an object of the provided type</returns>
        private static Func<object, object> CreateShallowFieldBasedCloner(Type clonedType)
        {
            var original = Expression.Parameter(typeof(object), "original");
            var transferExpressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            if (clonedType.IsPrimitive || clonedType.IsValueType || (clonedType == typeof(string)))
            {
                transferExpressions.Add(original);
            }
            else if (clonedType.IsArray)
            {
                transferExpressions.Add(GenerateFieldBasedPrimitiveArrayTransferExpressions(clonedType, original, variables, transferExpressions));
            }
            else
            {
                var clone = Expression.Variable(clonedType);
                variables.Add(clone);
                var typedOriginal = Expression.Variable(clonedType);
                variables.Add(typedOriginal);
                transferExpressions.Add(Expression.Assign(typedOriginal, Expression.Convert(original, clonedType))); var getUninitializedObjectMethodInfo = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Static | BindingFlags.Public);
                transferExpressions.Add(Expression.Assign(clone, Expression.Convert(Expression.Call(getUninitializedObjectMethodInfo, Expression.Constant(clonedType)), clonedType)));
                var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(clonedType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfos.ToList().ForEach(f =>
                {
                    transferExpressions.Add(Expression.Assign(Expression.Field(clone, f), Expression.Field(typedOriginal, f)));
                });
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

        #endregion CreateShallowFieldBasedCloner

        #region GenerateFieldBasedPrimitiveArrayTransferExpressions

        /// <summary>
        ///   Generates state transfer expressions to copy an array of primitive types
        /// </summary>
        /// <param name="clonedType">Type of array that will be cloned</param>
        /// <param name="original">Variable expression for the original array</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        /// <returns>The variable holding the cloned array</returns>
        private static Expression GenerateFieldBasedPrimitiveArrayTransferExpressions(Type clonedType, Expression original, ICollection<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var arrayCloneMethodInfo = typeof(Array).GetMethod("Clone");
            return Expression.Convert(Expression.Call(Expression.Convert(original, typeof(Array)), arrayCloneMethodInfo), clonedType);
        }

        #endregion GenerateFieldBasedPrimitiveArrayTransferExpressions

        #region GenerateFieldBasedComplexArrayTransferExpressions

        /// <summary>
        ///   Generates state transfer expressions to copy an array of complex types
        /// </summary>
        /// <param name="clonedType">Type of array that will be cloned</param>
        /// <param name="original">Variable expression for the original array</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        /// <returns>The variable holding the cloned array</returns>
        private static ParameterExpression GenerateFieldBasedComplexArrayTransferExpressions(Type clonedType, Expression original, IList<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var clone = Expression.Variable(clonedType);
            variables.Add(clone);
            var dimensionCount = clonedType.GetArrayRank();
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
            for (var index = dimensionCount - 1; index >= 0; --index)
            {
                var loopVariables = new List<ParameterExpression>();
                var loopExpressions = new List<Expression>();

                loopExpressions.Add(Expression.IfThen(Expression.GreaterThanOrEqual(indexes[index], lengths[index]), Expression.Break(labels[index])));

                if (innerLoop == null)
                {
                    if (elementType.IsPrimitive || (elementType == typeof(string)))
                    {
                        loopExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), Expression.ArrayAccess(original, indexes)));
                    }
                    else if (elementType.IsValueType)
                    {
                        GenerateFieldBasedComplexTypeTransferExpressions(elementType, Expression.ArrayAccess(original, indexes), Expression.ArrayAccess(clone, indexes), variables, loopExpressions);
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
                                clonedElement = GenerateFieldBasedPrimitiveArrayTransferExpressions(elementType, originalElement, nestedVariables, nestedTransferExpressions);
                            }
                            else
                            {
                                clonedElement = GenerateFieldBasedComplexArrayTransferExpressions(elementType, originalElement, nestedVariables, nestedTransferExpressions);
                            }
                            nestedTransferExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), clonedElement));
                        }
                        else
                        {
                            var getOrCreateClonerMethodInfo = typeof(ExpressionTreeCloner).GetMethod("GetOrCreateDeepFieldBasedCloner", BindingFlags.NonPublic | BindingFlags.Static);
                            var getTypeMethodInfo = typeof(object).GetMethod("GetType");
                            var invokeMethodInfo = typeof(Func<object, object>).GetMethod("Invoke");

                            nestedTransferExpressions.Add(Expression.Assign(Expression.ArrayAccess(clone, indexes), Expression.Convert(Expression.Call(Expression.Call(getOrCreateClonerMethodInfo, Expression.Call(originalElement, getTypeMethodInfo)), invokeMethodInfo, originalElement), elementType)));
                        }

                        loopExpressions.Add(Expression.IfThen(Expression.NotEqual(originalElement, Expression.Constant(null)), Expression.Block(nestedVariables, nestedTransferExpressions)));
                    }
                }
                else
                {
                    loopExpressions.Add(Expression.Assign(indexes[index + 1], Expression.Constant(0)));
                    loopExpressions.Add(innerLoop);
                }

                loopExpressions.Add(Expression.PreIncrementAssign(indexes[index]));

                innerLoop = Expression.Loop(Expression.Block(loopVariables, loopExpressions), labels[index]);
            }

            transferExpressions.Add(innerLoop);

            return clone;
        }

        #endregion GenerateFieldBasedComplexArrayTransferExpressions

        #region GenerateFieldBasedComplexTypeTransferExpressions

        /// <summary>Generates state transfer expressions to copy a complex type</summary>
        /// <param name="clonedType">Complex type that will be cloned</param>
        /// <param name="original">Variable expression for the original instance</param>
        /// <param name="clone">Variable expression for the cloned instance</param>
        /// <param name="variables">Receives variables used by the transfer expressions</param>
        /// <param name="transferExpressions">Receives the generated transfer expressions</param>
        private static void GenerateFieldBasedComplexTypeTransferExpressions(Type clonedType, Expression original, Expression clone, IList<ParameterExpression> variables, ICollection<Expression> transferExpressions)
        {
            var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(clonedType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfos.ToList().ForEach(f =>
            {
                var fieldType = f.FieldType;

                if (fieldType.IsPrimitive || (fieldType == typeof(string)))
                {
                    transferExpressions.Add(Expression.Assign(Expression.Field(clone, f), Expression.Field(original, f)));
                }
                else if (fieldType.IsValueType)
                {
                    GenerateFieldBasedComplexTypeTransferExpressions(fieldType, Expression.Field(original, f), Expression.Field(clone, f), variables, transferExpressions);
                }
                else
                {
                    GenerateFieldBasedReferenceTypeTransferExpressions(original, clone, transferExpressions, f, fieldType);
                }
            });
        }

        #endregion GenerateFieldBasedComplexTypeTransferExpressions

        #region GenerateFieldBasedReferenceTypeTransferExpressions

        /// <summary>
        ///   Generates the expressions to transfer a reference type (array or class)
        /// </summary>
        /// <param name="original">Original value that will be cloned</param>
        /// <param name="clone">Variable that will receive the cloned value</param>
        /// <param name="transferExpressions">
        ///   Receives the expression generated to transfer the values
        /// </param>
        /// <param name="fieldInfo">Reflection informations about the field being cloned</param>
        /// <param name="fieldType">Type of the field being cloned</param>
        private static void GenerateFieldBasedReferenceTypeTransferExpressions(Expression original, Expression clone, ICollection<Expression> transferExpressions, FieldInfo fieldInfo, Type fieldType)
        {
            var fieldTransferExpressions = new List<Expression>();
            var fieldVariables = new List<ParameterExpression>();

            if (fieldType.IsArray)
            {
                Expression fieldClone;

                var elementType = fieldType.GetElementType();
                if (elementType.IsPrimitive || (elementType == typeof(string)))
                {
                    fieldClone = GenerateFieldBasedPrimitiveArrayTransferExpressions(fieldType, Expression.Field(original, fieldInfo), fieldVariables, fieldTransferExpressions);
                }
                else
                {
                    fieldClone = GenerateFieldBasedComplexArrayTransferExpressions(fieldType, Expression.Field(original, fieldInfo), fieldVariables, fieldTransferExpressions);
                }

                fieldTransferExpressions.Add(Expression.Assign(Expression.Field(clone, fieldInfo), fieldClone));
            }
            else
            {
                var getOrCreateClonerMethodInfo = typeof(ExpressionTreeCloner).GetMethod("GetOrCreateDeepFieldBasedCloner", BindingFlags.NonPublic | BindingFlags.Static);
                var getTypeMethodInfo = typeof(object).GetMethod("GetType");
                var invokeMethodInfo = typeof(Func<object, object>).GetMethod("Invoke");

                fieldTransferExpressions.Add(Expression.Assign(Expression.Field(clone, fieldInfo), Expression.Convert(Expression.Call(Expression.Call(getOrCreateClonerMethodInfo, Expression.Call(Expression.Field(original, fieldInfo), getTypeMethodInfo)), invokeMethodInfo, Expression.Field(original, fieldInfo)), fieldType)));
            }

            transferExpressions.Add(Expression.IfThen(Expression.NotEqual(Expression.Field(original, fieldInfo), Expression.Constant(null)), Expression.Block(fieldVariables, fieldTransferExpressions)));
        }

        #endregion GenerateFieldBasedReferenceTypeTransferExpressions
    }
}