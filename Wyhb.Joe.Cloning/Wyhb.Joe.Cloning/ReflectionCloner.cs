using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Wyhb.Joe.Cloning
{
    /// <summary>Clones objects using reflection</summary>
    /// <remarks>
    ///   <para>
    ///     This type of cloning is a lot faster than cloning by serialization and
    ///     incurs no set-up cost, but requires cloned types to provide a default
    ///     constructor in order to work.
    ///   </para>
    /// </remarks>
    public class ReflectionCloner : ICloneFactory
    {
        #region ShallowFieldClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        public static TCloned ShallowFieldClone<TCloned>(TCloned objectToClone)
        {
            var originalType = objectToClone.GetType();
            if (originalType.IsPrimitive || (originalType == typeof(string)))
            {
                return objectToClone;
            }
            else if (originalType.IsArray)
            {
                return (TCloned)ShallowCloneArray(objectToClone);
            }
            else if (originalType.IsValueType)
            {
                return objectToClone;
            }
            else
            {
                return (TCloned)ShallowCloneComplexFieldBased(objectToClone);
            }
        }

        #endregion ShallowFieldClone

        #region ShallowPropertyClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        public static TCloned ShallowPropertyClone<TCloned>(TCloned objectToClone)
        {
            var originalType = objectToClone.GetType();
            if (originalType.IsPrimitive || (originalType == typeof(string)))
            {
                return objectToClone; // Being value types, primitives are copied by default
            }
            else if (originalType.IsArray)
            {
                return (TCloned)ShallowCloneArray(objectToClone);
            }
            else if (originalType.IsValueType)
            {
                return (TCloned)ShallowCloneComplexPropertyBased(objectToClone);
            }
            else
            {
                return (TCloned)ShallowCloneComplexPropertyBased(objectToClone);
            }
        }

        #endregion ShallowPropertyClone

        #region DeepFieldClone

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        public static TCloned DeepFieldClone<TCloned>(TCloned objectToClone)
        {
            var objectToCloneAsObject = objectToClone;
            if (objectToClone == null)
            {
                return default(TCloned);
            }
            else
            {
                return (TCloned)DeepCloneSingleFieldBased(objectToCloneAsObject);
            }
        }

        #endregion DeepFieldClone

        #region DeepPropertyClone

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        public static TCloned DeepPropertyClone<TCloned>(TCloned objectToClone)
        {
            var objectToCloneAsObject = objectToClone;
            if (objectToClone == null)
            {
                return default(TCloned);
            }
            else
            {
                return (TCloned)DeepCloneSinglePropertyBased(objectToCloneAsObject);
            }
        }

        #endregion DeepPropertyClone

        #region ICloneFactory.ShallowFieldClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        TCloned ICloneFactory.ShallowFieldClone<TCloned>(TCloned objectToClone)
        {
            if (typeof(TCloned).IsClass || typeof(TCloned).IsArray)
            {
                if (ReferenceEquals(objectToClone, null))
                {
                    return default(TCloned);
                }
            }
            return ReflectionCloner.ShallowFieldClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.ShallowFieldClone

        #region ICloneFactory.ShallowPropertyClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        TCloned ICloneFactory.ShallowPropertyClone<TCloned>(TCloned objectToClone)
        {
            if (typeof(TCloned).IsClass || typeof(TCloned).IsArray)
            {
                if (ReferenceEquals(objectToClone, null))
                {
                    return default(TCloned);
                }
            }
            return ReflectionCloner.ShallowPropertyClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.ShallowPropertyClone

        #region ICloneFactory.DeepFieldClone

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        TCloned ICloneFactory.DeepFieldClone<TCloned>(TCloned objectToClone)
        {
            return ReflectionCloner.DeepFieldClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.DeepFieldClone

        #region ICloneFactory.DeepPropertyClone

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        TCloned ICloneFactory.DeepPropertyClone<TCloned>(TCloned objectToClone)
        {
            return ReflectionCloner.DeepPropertyClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.DeepPropertyClone

        #region ShallowCloneComplexFieldBased

        /// <summary>Clones a complex type using field-based value transfer</summary>
        /// <param name="original">Original instance that will be cloned</param>
        /// <returns>A clone of the original instance</returns>
        private static object ShallowCloneComplexFieldBased(object original)
        {
            var originalType = original.GetType();
#if (XBOX360 || WINDOWS_PHONE)
      object clone = Activator.CreateInstance(originalType);
#else
            object clone = FormatterServices.GetUninitializedObject(originalType);
#endif

            var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(originalType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfos.ToList().ForEach(f =>
            {
                object originalValue = f.GetValue(original);
                if (originalValue != null)
                {
                    f.SetValue(clone, originalValue);
                }
            });

            return clone;
        }

        #endregion ShallowCloneComplexFieldBased

        #region ShallowCloneComplexPropertyBased

        /// <summary>Clones a complex type using property-based value transfer</summary>
        /// <param name="original">Original instance that will be cloned</param>
        /// <returns>A clone of the original instance</returns>
        private static object ShallowCloneComplexPropertyBased(object original)
        {
            var originalType = original.GetType();
            var clone = Activator.CreateInstance(originalType);

            var propertyInfos = originalType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            propertyInfos.ToList().ForEach(p =>
            {
                if (p.CanRead && p.CanWrite)
                {
                    var propertyType = p.PropertyType;
                    var originalValue = p.GetValue(original, null);
                    if (originalValue != null)
                    {
                        if (propertyType.IsPrimitive || (propertyType == typeof(string)))
                        {
                            p.SetValue(clone, originalValue, null);
                        }
                        else if (propertyType.IsValueType)
                        {
                            p.SetValue(clone, ShallowCloneComplexPropertyBased(originalValue), null);
                        }
                        else if (propertyType.IsArray)
                        {
                            p.SetValue(clone, originalValue, null);
                        }
                        else
                        {
                            p.SetValue(clone, originalValue, null);
                        }
                    }
                }
            });

            return clone;
        }

        #endregion ShallowCloneComplexPropertyBased

        #region ShallowCloneArray

        /// <summary>Clones an array using field-based value transfer</summary>
        /// <param name="original">Original array that will be cloned</param>
        /// <returns>A clone of the original array</returns>
        private static object ShallowCloneArray(object original)
        {
            return ((Array)original).Clone();
        }

        #endregion ShallowCloneArray

        #region DeepCloneSingleFieldBased

        /// <summary>Copies a single object using field-based value transfer</summary>
        /// <param name="original">Original object that will be cloned</param>
        /// <returns>A clone of the original object</returns>
        private static object DeepCloneSingleFieldBased(object original)
        {
            var originalType = original.GetType();
            if (originalType.IsPrimitive || (originalType == typeof(string)))
            {
                return original;
            }
            else if (originalType.IsArray)
            {
                return DeepCloneArrayFieldBased((Array)original, originalType.GetElementType());
            }
            else
            {
                return DeepCloneComplexFieldBased(original);
            }
        }

        #endregion DeepCloneSingleFieldBased

        #region DeepCloneComplexFieldBased

        /// <summary>Clones a complex type using field-based value transfer</summary>
        /// <param name="original">Original instance that will be cloned</param>
        /// <returns>A clone of the original instance</returns>
        private static object DeepCloneComplexFieldBased(object original)
        {
            var originalType = original.GetType();
#if (XBOX360 || WINDOWS_PHONE)
            var clone = Activator.CreateInstance(originalType);
#else
            var clone = FormatterServices.GetUninitializedObject(originalType);
#endif

            var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(originalType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfos.ToList().ForEach(f =>
            {
                var fieldType = f.FieldType;
                object originalValue = f.GetValue(original);
                if (originalValue != null)
                {
                    if (fieldType.IsPrimitive || (fieldType == typeof(string)))
                    {
                        f.SetValue(clone, originalValue);
                    }
                    else if (fieldType.IsArray)
                    {
                        f.SetValue(clone, DeepCloneArrayFieldBased((Array)originalValue, fieldType.GetElementType()));
                    }
                    else
                    {
                        f.SetValue(clone, DeepCloneSingleFieldBased(originalValue));
                    }
                }
            });

            return clone;
        }

        #endregion DeepCloneComplexFieldBased

        #region DeepCloneArrayFieldBased

        /// <summary>Clones an array using field-based value transfer</summary>
        /// <param name="original">Original array that will be cloned</param>
        /// <param name="elementType">Type of elements the original array contains</param>
        /// <returns>A clone of the original array</returns>
        private static object DeepCloneArrayFieldBased(Array original, Type elementType)
        {
            if (elementType.IsPrimitive || (elementType == typeof(string)))
            {
                return original.Clone();
            }

            var dimensionCount = original.Rank;

            var lengths = new int[dimensionCount];
            var totalElementCount = 0;
            for (var idx = 0; idx < dimensionCount; ++idx)
            {
                lengths[idx] = original.GetLength(idx);
                if (idx == 0)
                {
                    totalElementCount = lengths[idx];
                }
                else
                {
                    totalElementCount *= lengths[idx];
                }
            }

            var clone = Array.CreateInstance(elementType, lengths);

            if (dimensionCount == 1)
            {
                for (var idx = 0; idx < totalElementCount; ++idx)
                {
                    var originalElement = original.GetValue(idx);
                    if (originalElement != null)
                    {
                        clone.SetValue(DeepCloneSingleFieldBased(originalElement), idx);
                    }
                }
            }
            else
            {
                var indices = new int[dimensionCount];
                for (var idx = 0; idx < totalElementCount; ++idx)
                {
                    var elementIndex = idx;
                    for (var dimensionIndex = dimensionCount - 1; dimensionIndex >= 0; --dimensionIndex)
                    {
                        indices[dimensionIndex] = elementIndex % lengths[dimensionIndex];
                        elementIndex /= lengths[dimensionIndex];
                    }

                    object originalElement = original.GetValue(indices);
                    if (originalElement != null)
                    {
                        clone.SetValue(DeepCloneSingleFieldBased(originalElement), indices);
                    }
                }
            }

            return clone;
        }

        #endregion DeepCloneArrayFieldBased

        #region DeepCloneSinglePropertyBased

        /// <summary>Copies a single object using property-based value transfer</summary>
        /// <param name="original">Original object that will be cloned</param>
        /// <returns>A clone of the original object</returns>
        private static object DeepCloneSinglePropertyBased(object original)
        {
            var originalType = original.GetType();
            if (originalType.IsPrimitive || (originalType == typeof(string)))
            {
                return original;
            }
            else if (originalType.IsArray)
            {
                return DeepCloneArrayPropertyBased((Array)original, originalType.GetElementType());
            }
            else
            {
                return DeepCloneComplexPropertyBased(original);
            }
        }

        #endregion DeepCloneSinglePropertyBased

        #region DeepCloneComplexPropertyBased

        /// <summary>Clones a complex type using property-based value transfer</summary>
        /// <param name="original">Original instance that will be cloned</param>
        /// <returns>A clone of the original instance</returns>
        private static object DeepCloneComplexPropertyBased(object original)
        {
            var originalType = original.GetType();
            var clone = Activator.CreateInstance(originalType);

            var propertyInfos = originalType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            propertyInfos.ToList().ForEach(p =>
            {
                if (p.CanRead && p.CanWrite)
                {
                    var propertyType = p.PropertyType;
                    var originalValue = p.GetValue(original, null);
                    if (originalValue != null)
                    {
                        if (propertyType.IsPrimitive || (propertyType == typeof(string)))
                        {
                            p.SetValue(clone, originalValue, null);
                        }
                        else if (propertyType.IsArray)
                        {
                            p.SetValue(clone, DeepCloneArrayPropertyBased((Array)originalValue, propertyType.GetElementType()), null);
                        }
                        else
                        {
                            p.SetValue(clone, DeepCloneSinglePropertyBased(originalValue), null);
                        }
                    }
                }
            });
            for (var idx = 0; idx < propertyInfos.Length; ++idx)
            {
            }

            return clone;
        }

        #endregion DeepCloneComplexPropertyBased

        #region DeepCloneArrayPropertyBased

        /// <summary>Clones an array using property-based value transfer</summary>
        /// <param name="original">Original array that will be cloned</param>
        /// <param name="elementType">Type of elements the original array contains</param>
        /// <returns>A clone of the original array</returns>
        private static object DeepCloneArrayPropertyBased(Array original, Type elementType)
        {
            if (elementType.IsPrimitive || (elementType == typeof(string)))
            {
                return original.Clone();
            }

            var dimensionCount = original.Rank;

            var lengths = new int[dimensionCount];
            var totalElementCount = 0;
            for (var idx = 0; idx < dimensionCount; ++idx)
            {
                lengths[idx] = original.GetLength(idx);
                if (idx == 0)
                {
                    totalElementCount = lengths[idx];
                }
                else
                {
                    totalElementCount *= lengths[idx];
                }
            }

            var clone = Array.CreateInstance(elementType, lengths);

            if (dimensionCount == 1)
            {
                for (var idx = 0; idx < totalElementCount; ++idx)
                {
                    var originalElement = original.GetValue(idx);
                    if (originalElement != null)
                    {
                        clone.SetValue(DeepCloneSinglePropertyBased(originalElement), idx);
                    }
                }
            }
            else
            {
                var indices = new int[dimensionCount];
                for (var idx = 0; idx < totalElementCount; ++idx)
                {
                    var elementIndex = idx;
                    for (var dimensionIndex = dimensionCount - 1; dimensionIndex >= 0; --dimensionIndex)
                    {
                        indices[dimensionIndex] = elementIndex % lengths[dimensionIndex];
                        elementIndex /= lengths[dimensionIndex];
                    }

                    var originalElement = original.GetValue(indices);
                    if (originalElement != null)
                    {
                        clone.SetValue(DeepCloneSinglePropertyBased(originalElement), indices);
                    }
                }
            }

            return clone;
        }

        #endregion DeepCloneArrayPropertyBased
    }
}