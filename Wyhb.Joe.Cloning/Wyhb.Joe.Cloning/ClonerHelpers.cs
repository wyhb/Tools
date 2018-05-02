using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Wyhb.Joe.Cloning
{
    /// <summary>Contains helper methods for the cloners</summary>
    public static class ClonerHelpers
    {
        #region GetFieldInfosIncludingBaseClasses

        /// <summary>
        ///   Returns all the fields of a type, working around a weird reflection issue
        ///   where explicitly declared fields in base classes are returned, but not
        ///   automatic property backing fields.
        /// </summary>
        /// <param name="type">Type whose fields will be returned</param>
        /// <param name="bindingFlags">Binding flags to use when querying the fields</param>
        /// <returns>All of the type's fields, including its base types</returns>
        public static FieldInfo[] GetFieldInfosIncludingBaseClasses(Type type, BindingFlags bindingFlags)
        {
            var fieldInfos = type.GetFields(bindingFlags);

            if (type.BaseType == typeof(object))
            {
                return fieldInfos;
            }
            else
            {
                var fieldInfoList = new List<FieldInfo>(fieldInfos);
                while (type.BaseType != typeof(object))
                {
                    type = type.BaseType;
                    type.GetFields(bindingFlags).ToList().ForEach(f =>
                    {
                        bool found = false;

                        for (var searchIdx = 0; searchIdx < fieldInfoList.Count; ++searchIdx)
                        {
                            bool match = (fieldInfoList[searchIdx].DeclaringType == f.DeclaringType) && (fieldInfoList[searchIdx].Name == f.Name);

                            if (match)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            fieldInfoList.Add(f);
                        }
                    });
                }

                return fieldInfoList.ToArray();
            }
        }

        #endregion GetFieldInfosIncludingBaseClasses
    }
}