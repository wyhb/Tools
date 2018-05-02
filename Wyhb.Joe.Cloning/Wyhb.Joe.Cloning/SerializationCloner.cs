using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Wyhb.Joe.Cloning
{
    /// <summary>Clones objects via serialization</summary>
    /// <remarks>
    ///   <para>
    ///     This type of cloning uses the binary formatter to persist the state of
    ///     an object and then restores it into a clone. It has the advantage of even
    ///     working with types that don't provide a default constructor, but is
    ///     terribly slow.
    ///   </para>
    ///   <para>
    ///     Inspired by the "A Generic Method for Deep Cloning in C# 3.0" article
    ///     on CodeProject: http://www.codeproject.com/KB/cs/generic_deep_cloning.aspx
    ///   </para>
    /// </remarks>
    public class SerializationCloner : ICloneFactory
    {
        #region class StaticSurrogateSelector

        /// <summary>Selects a static surrogate for any non-primitive types</summary>
        private class StaticSurrogateSelector : ISurrogateSelector
        {
            /// <summary>Initializes a new static surrogate selector</summary>
            /// <param name="staticSurrogate">Surrogate that will be selected</param>
            public StaticSurrogateSelector(ISerializationSurrogate staticSurrogate)
            {
                this.staticSurrogate = staticSurrogate;
            }

            /// <summary>
            ///   Sets the next selector to escalate to if this one can't provide a surrogate
            /// </summary>
            /// <param name="selector">Selector to escalate to</param>
            public void ChainSelector(ISurrogateSelector selector)
            {
                this.chainedSelector = selector;
            }

            /// <summary>
            ///   Returns the selector this one will escalate to if it can't provide a surrogate
            /// </summary>
            /// <returns>The selector this one will escalate to</returns>
            public ISurrogateSelector GetNextSelector()
            {
                return this.chainedSelector;
            }

            /// <summary>Attempts to provides a surrogate for the specified type</summary>
            /// <param name="type">Type a surrogate will be provided for</param>
            /// <param name="context">Context </param>
            /// <param name="selector"></param>
            /// <returns></returns>
            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                if (type.IsPrimitive || type.IsArray || (type == typeof(string)))
                {
                    if (this.chainedSelector == null)
                    {
                        selector = null;
                        return null;
                    }
                    else
                    {
                        return this.chainedSelector.GetSurrogate(type, context, out selector);
                    }
                }
                else
                {
                    selector = this;
                    return this.staticSurrogate;
                }
            }

            /// <summary>Surrogate the that will be selected for any non-primitive types</summary>
            private ISerializationSurrogate staticSurrogate;

            /// <summary>Surrogate selector to escalate to if no surrogate can be provided</summary>
            private ISurrogateSelector chainedSelector;
        }

        #endregion class StaticSurrogateSelector

        #region class FieldSerializationSurrogate

        /// <summary>Serializes a type based on its fields</summary>
        private class FieldSerializationSurrogate : ISerializationSurrogate
        {
            /// <summary>Extracts the data to be serialized from an object</summary>
            /// <param name="objectToSerialize">Object that is being serialized</param>
            /// <param name="info">Stores the serialized informations</param>
            /// <param name="context">
            ///   Provides additional informations about the serialization process
            /// </param>
            public void GetObjectData(object objectToSerialize, SerializationInfo info, StreamingContext context)
            {
                var originalType = objectToSerialize.GetType();

                var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(originalType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfos.ToList().ForEach(f =>
                {
                    info.AddValue(f.Name, f.GetValue(objectToSerialize));
                });
            }

            /// <summary>Reinserts saved data into a deserializd object</summary>
            /// <param name="deserializedObject">Object the saved data will be inserted into</param>
            /// <param name="info">Contains the serialized informations</param>
            /// <param name="context">
            ///   Provides additional informations about the serialization process
            /// </param>
            /// <param name="selector">Surrogate selector that specified this surrogate</param>
            /// <returns>The deserialized object</returns>
            public object SetObjectData(object deserializedObject, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var originalType = deserializedObject.GetType();

                var fieldInfos = ClonerHelpers.GetFieldInfosIncludingBaseClasses(originalType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfos.ToList().ForEach(f =>
                {
                    f.SetValue(deserializedObject, info.GetValue(f.Name, f.FieldType));
                });

                return deserializedObject;
            }
        }

        #endregion class FieldSerializationSurrogate

        #region class PropertySerializationSurrogate

        /// <summary>Serializes a type based on its properties</summary>
        private class PropertySerializationSurrogate : ISerializationSurrogate
        {
            /// <summary>Extracts the data to be serialized from an object</summary>
            /// <param name="objectToSerialize">Object that is being serialized</param>
            /// <param name="info">Stores the serialized informations</param>
            /// <param name="context">
            ///   Provides additional informations about the serialization process
            /// </param>
            public void GetObjectData(
              object objectToSerialize,
              SerializationInfo info,
              StreamingContext context
            )
            {
                Type originalType = objectToSerialize.GetType();

                PropertyInfo[] propertyInfos = originalType.GetProperties(
                  BindingFlags.Public | BindingFlags.NonPublic |
                  BindingFlags.Instance | BindingFlags.FlattenHierarchy
                );
                for (int index = 0; index < propertyInfos.Length; ++index)
                {
                    PropertyInfo propertyInfo = propertyInfos[index];
                    if (propertyInfo.CanRead && propertyInfo.CanWrite)
                    {
                        info.AddValue(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null));
                    }
                }
            }

            /// <summary>Reinserts saved data into a deserializd object</summary>
            /// <param name="deserializedObject">Object the saved data will be inserted into</param>
            /// <param name="info">Contains the serialized informations</param>
            /// <param name="context">
            ///   Provides additional informations about the serialization process
            /// </param>
            /// <param name="selector">Surrogate selector that specified this surrogate</param>
            /// <returns>The deserialized object</returns>
            public object SetObjectData(
              object deserializedObject,
              SerializationInfo info,
              StreamingContext context,
              ISurrogateSelector selector
            )
            {
                Type originalType = deserializedObject.GetType();

                PropertyInfo[] propertyInfos = originalType.GetProperties(
                  BindingFlags.Public | BindingFlags.NonPublic |
                  BindingFlags.Instance | BindingFlags.FlattenHierarchy
                );
                for (int index = 0; index < propertyInfos.Length; ++index)
                {
                    PropertyInfo propertyInfo = propertyInfos[index];
                    if (propertyInfo.CanRead && propertyInfo.CanWrite)
                    {
                        propertyInfo.SetValue(
                          deserializedObject,
                          info.GetValue(propertyInfo.Name, propertyInfo.PropertyType),
                          null
                        );
                    }
                }

                return deserializedObject;
            }
        }

        #endregion class PropertySerializationSurrogate

        #region SerializationCloner

        /// <summary>Initializes the static members of the serialization-based cloner</summary>
        static SerializationCloner()
        {
            FieldBasedFormatter = new BinaryFormatter(new StaticSurrogateSelector(new FieldSerializationSurrogate()), new StreamingContext(StreamingContextStates.All));
            PropertyBasedFormatter = new BinaryFormatter(new StaticSurrogateSelector(new PropertySerializationSurrogate()), new StreamingContext(StreamingContextStates.All));
        }

        #endregion SerializationCloner

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
            if (typeof(TCloned).IsClass || typeof(TCloned).IsArray)
            {
                if (ReferenceEquals(objectToClone, null))
                {
                    return default(TCloned);
                }
            }
            using (var memoryStream = new MemoryStream())
            {
                FieldBasedFormatter.Serialize(memoryStream, objectToClone);
                memoryStream.Position = 0;
                return (TCloned)FieldBasedFormatter.Deserialize(memoryStream);
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
            if (typeof(TCloned).IsClass || typeof(TCloned).IsArray)
            {
                if (ReferenceEquals(objectToClone, null))
                {
                    return default(TCloned);
                }
            }
            using (var memoryStream = new MemoryStream())
            {
                PropertyBasedFormatter.Serialize(memoryStream, objectToClone);
                memoryStream.Position = 0;
                return (TCloned)PropertyBasedFormatter.Deserialize(memoryStream);
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
            throw new NotSupportedException("The serialization cloner cannot create shallow clones");
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
            throw new NotSupportedException("The serialization cloner cannot create shallow clones");
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
            return SerializationCloner.DeepFieldClone<TCloned>(objectToClone);
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
            return SerializationCloner.DeepPropertyClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.DeepPropertyClone

        #region FieldBasedFormatter

        /// <summary>Serializes objects by storing their fields</summary>
        private static BinaryFormatter FieldBasedFormatter;

        #endregion FieldBasedFormatter

        #region PropertyBasedFormatter

        /// <summary>Serializes objects by storing their properties</summary>
        private static BinaryFormatter PropertyBasedFormatter;

        #endregion PropertyBasedFormatter
    }
}