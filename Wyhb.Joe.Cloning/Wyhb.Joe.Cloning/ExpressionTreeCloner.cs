using System;
using System.Collections.Concurrent;

namespace Wyhb.Joe.Cloning
{
    /// <summary>
    ///   Cloning factory which uses expression trees to improve performance when cloning
    ///   is a high-frequency action.
    /// </summary>
    public partial class ExpressionTreeCloner : ICloneFactory
    {
        #region ExpressionTreeCloner

        /// <summary>Initializes the static members of the expression tree cloner</summary>
        static ExpressionTreeCloner()
        {
            ShallowFieldBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            DeepFieldBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            ShallowPropertyBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            DeepPropertyBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
        }

        #endregion ExpressionTreeCloner

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
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            return (TCloned)GetOrCreateDeepFieldBasedCloner(typeof(TCloned))(objectToCloneAsObject);
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
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            return (TCloned)GetOrCreateDeepPropertyBasedCloner(typeof(TCloned))(objectToCloneAsObject);
        }

        #endregion DeepPropertyClone

        #region ShallowFieldClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        public static TCloned ShallowFieldClone<TCloned>(TCloned objectToClone)
        {
            var objectToCloneAsObject = objectToClone;
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            return (TCloned)GetOrCreateShallowFieldBasedCloner(typeof(TCloned))(objectToCloneAsObject);
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
            object objectToCloneAsObject = objectToClone;
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            return (TCloned)GetOrCreateShallowPropertyBasedCloner(typeof(TCloned))(objectToCloneAsObject);
        }

        #endregion ShallowPropertyClone

        #region ICloneFactory.ShallowFieldClone

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        TCloned ICloneFactory.ShallowFieldClone<TCloned>(TCloned objectToClone)
        {
            return ExpressionTreeCloner.ShallowFieldClone<TCloned>(objectToClone);
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
            return ExpressionTreeCloner.ShallowPropertyClone<TCloned>(objectToClone);
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
            return ExpressionTreeCloner.DeepFieldClone<TCloned>(objectToClone);
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
            return ExpressionTreeCloner.DeepPropertyClone<TCloned>(objectToClone);
        }

        #endregion ICloneFactory.DeepPropertyClone

        #region GetOrCreateShallowFieldBasedCloner

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> GetOrCreateShallowFieldBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!ShallowFieldBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = CreateShallowFieldBasedCloner(clonedType);
                ShallowFieldBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        #endregion GetOrCreateShallowFieldBasedCloner

        #region GetOrCreateDeepFieldBasedCloner

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> GetOrCreateDeepFieldBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!DeepFieldBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = CreateDeepFieldBasedCloner(clonedType);
                DeepFieldBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        #endregion GetOrCreateDeepFieldBasedCloner

        #region GetOrCreateShallowPropertyBasedCloner

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> GetOrCreateShallowPropertyBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!ShallowPropertyBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = CreateShallowPropertyBasedCloner(clonedType);
                ShallowPropertyBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        #endregion GetOrCreateShallowPropertyBasedCloner

        #region GetOrCreateDeepPropertyBasedCloner

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> GetOrCreateDeepPropertyBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!DeepPropertyBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = CreateDeepPropertyBasedCloner(clonedType);
                DeepPropertyBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        #endregion GetOrCreateDeepPropertyBasedCloner

        #region ShallowFieldBasedCloners

        /// <summary>Compiled cloners that perform shallow clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> ShallowFieldBasedCloners;

        #endregion ShallowFieldBasedCloners

        #region DeepFieldBasedCloners

        /// <summary>Compiled cloners that perform deep clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> DeepFieldBasedCloners;

        #endregion DeepFieldBasedCloners

        #region ShallowPropertyBasedCloners

        /// <summary>Compiled cloners that perform shallow clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> ShallowPropertyBasedCloners;

        #endregion ShallowPropertyBasedCloners

        #region DeepPropertyBasedCloners

        /// <summary>Compiled cloners that perform deep clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> DeepPropertyBasedCloners;

        #endregion DeepPropertyBasedCloners
    }
}