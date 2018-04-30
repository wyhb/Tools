﻿using System;
using System.Collections.Concurrent;

namespace Wyhb.Joe.Cloning
{
    /// <summary>
    ///   Cloning factory which uses expression trees to improve performance when cloning
    ///   is a high-frequency action.
    /// </summary>
    public partial class ExpressionTreeCloner : ICloneFactory
    {
        /// <summary>Initializes the static members of the expression tree cloner</summary>
        static ExpressionTreeCloner()
        {
            shallowFieldBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            deepFieldBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            shallowPropertyBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
            deepPropertyBasedCloners = new ConcurrentDictionary<Type, Func<object, object>>();
        }

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        public static TCloned DeepFieldClone<TCloned>(TCloned objectToClone)
        {
            object objectToCloneAsObject = objectToClone;
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            Func<object, object> cloner = getOrCreateDeepFieldBasedCloner(typeof(TCloned));
            return (TCloned)cloner(objectToCloneAsObject);
        }

        /// <summary>
        ///   Creates a deep clone of the specified object, also creating clones of all
        ///   child objects being referenced
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A deep clone of the provided object</returns>
        public static TCloned DeepPropertyClone<TCloned>(TCloned objectToClone)
        {
            object objectToCloneAsObject = objectToClone;
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            Func<object, object> cloner = getOrCreateDeepPropertyBasedCloner(typeof(TCloned));
            return (TCloned)cloner(objectToCloneAsObject);
        }

        /// <summary>
        ///   Creates a shallow clone of the specified object, reusing any referenced objects
        /// </summary>
        /// <typeparam name="TCloned">Type of the object that will be cloned</typeparam>
        /// <param name="objectToClone">Object that will be cloned</param>
        /// <returns>A shallow clone of the provided object</returns>
        public static TCloned ShallowFieldClone<TCloned>(TCloned objectToClone)
        {
            object objectToCloneAsObject = objectToClone;
            if (objectToCloneAsObject == null)
            {
                return default(TCloned);
            }

            Func<object, object> cloner = getOrCreateShallowFieldBasedCloner(typeof(TCloned));
            return (TCloned)cloner(objectToCloneAsObject);
        }

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

            Func<object, object> cloner = getOrCreateShallowPropertyBasedCloner(typeof(TCloned));
            return (TCloned)cloner(objectToCloneAsObject);
        }

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

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> getOrCreateShallowFieldBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!shallowFieldBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = createShallowFieldBasedCloner(clonedType);
                shallowFieldBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> getOrCreateDeepFieldBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!deepFieldBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = createDeepFieldBasedCloner(clonedType);
                deepFieldBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> getOrCreateShallowPropertyBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!shallowPropertyBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = createShallowPropertyBasedCloner(clonedType);
                shallowPropertyBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        /// <summary>
        ///   Retrieves the existing clone method for the specified type or compiles one if
        ///   none exists for the type yet
        /// </summary>
        /// <param name="clonedType">Type for which a clone method will be retrieved</param>
        /// <returns>The clone method for the specified type</returns>
        private static Func<object, object> getOrCreateDeepPropertyBasedCloner(Type clonedType)
        {
            Func<object, object> cloner;

            if (!deepPropertyBasedCloners.TryGetValue(clonedType, out cloner))
            {
                cloner = createDeepPropertyBasedCloner(clonedType);
                deepPropertyBasedCloners.TryAdd(clonedType, cloner);
            }

            return cloner;
        }

        /// <summary>Compiled cloners that perform shallow clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> shallowFieldBasedCloners;

        /// <summary>Compiled cloners that perform deep clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> deepFieldBasedCloners;

        /// <summary>Compiled cloners that perform shallow clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> shallowPropertyBasedCloners;

        /// <summary>Compiled cloners that perform deep clone operations</summary>
        private static ConcurrentDictionary<Type, Func<object, object>> deepPropertyBasedCloners;
    }
}