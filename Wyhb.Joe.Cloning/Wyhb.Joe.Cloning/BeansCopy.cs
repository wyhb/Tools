using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wyhb.Joe.Cloning
{
    /// <summary>
    /// SourceとDestinationで指定されたクラスを相互にマッピングします
    /// </summary>
    /// <typeparam name="Source"></typeparam>
    /// <typeparam name="Destination"></typeparam>
    public class BeansCopy<Source, Destination> where Source : new() where Destination : new()
    {
        private class MappingInfo
        {
            public string Name { get; set; }
            public MappingEntity Source { get; set; }
            public MappingEntity Destination { get; set; }
        }

        private class MappingEntity
        {
            public MemberTypes MemberType { get; set; }
            public FieldInfo Field { get; set; }
            public PropertyInfo Property { get; set; }
        }

        private class EntityMapping<SourceType, DestinationType>
        {
            public List<MappingInfo> MappingInfoList { get; set; }

            public EntityMapping()
            {
                MappingInfoList = new List<MappingInfo>();
                CreateMapping();
            }

            public void CreateMapping()
            {
                Type sourceType = typeof(SourceType);
                Type destinationType = typeof(DestinationType);

                // 名前の取得
                var sourceList = GetPublicField(sourceType);
                var sourceNameList = sourceList.Select(x => x.Key).ToList();
                var destinationList = GetPublicField(destinationType);
                var destinationNameList = destinationList.Select(x => x.Key).ToList();

                // 同名のフィールド・プロパティのリストを作成
                sourceNameList.Where(x => destinationNameList.Contains(x)).ToList<string>().ForEach(x =>
                {
                    var info = new MappingInfo();
                    info.Name = x;
                    info.Source = GetDestination(sourceType, x, sourceList.Find(y => y.Key == x).Value);
                    info.Destination = GetDestination(destinationType, x, destinationList.Find(y => y.Key == x).Value);
                    MappingInfoList.Add(info);
                });
            }

            private List<KeyValuePair<string, MemberTypes>> GetPublicField(Type type)
            {
                var list = new List<MemberInfo>(type.GetMembers(BindingFlags.Public | BindingFlags.Instance));
                return list.Where(x => (x.MemberType == MemberTypes.Property) | (x.MemberType == MemberTypes.Field)).Select(x => new KeyValuePair<string, MemberTypes>(x.Name, x.MemberType))
                    .OrderBy(x => x.Value).ToList<KeyValuePair<string, MemberTypes>>();
            }

            private MappingEntity GetDestination(Type type, string memberName, MemberTypes memberType)
            {
                var Destination = new MappingEntity();
                Destination.MemberType = memberType;
                switch (memberType)
                {
                    case MemberTypes.Field:
                        Destination.Field = type.GetField(memberName);
                        break;

                    case MemberTypes.Property:
                        Destination.Property = type.GetProperty(memberName);
                        break;
                }
                return Destination;
            }
        }

        /// <summary>
        /// 対応するマッピング
        /// </summary>
        static private EntityMapping<Source, Destination> Mapping { get; set; }

        static private object lockObject = new Object();

        static BeansCopy()
        {
            lock (lockObject)
            {
                Mapping = new EntityMapping<Source, Destination>();
            }
        }

        /// <summary>
        /// SourceからDestinationにマッピングします
        /// </summary>
        /// <param name="Destination">マッピング元オブジェクト</param>
        /// <returns>マッピング先オブジェクト</returns>
        public static Source Map(Destination Destination)
        {
            var Source = new Source();
            Mapping.MappingInfoList.ForEach(x => SetValue(Source, x.Source, GetValue(Destination, x.Destination)));
            return Source;
        }

        /// <summary>
        /// SourceからDestinationにマッピングします
        /// </summary>
        /// <param name="DestinationLst">マッピング元オブジェクトリスト</param>
        /// <returns>マッピング先オブジェクト</returns>
        public static List<Source> Map(List<Destination> DestinationLst)
        {
            List<Source> SourceLst = new List<Source>();
            foreach (Destination Destination in DestinationLst)
            {
                var Source = new Source();
                Mapping.MappingInfoList.ForEach(x => SetValue(Source, x.Source, GetValue(Destination, x.Destination)));
                SourceLst.Add(Source);
            }
            return SourceLst;
        }

        /// <summary>
        /// DestinationからSourceにマッピングします
        /// </summary>
        /// <param name="Source">マッピング元オブジェクト</param>
        /// <returns>マッピング先オブジェクト</returns>
        public static Destination Map(Source Source)
        {
            var Destination = new Destination();
            Mapping.MappingInfoList.ForEach(x => SetValue(Destination, x.Destination, GetValue(Source, x.Source)));
            return Destination;
        }

        /// <summary>
        /// DestinationからSourceにマッピングします
        /// </summary>
        /// <param name="SourceLst">マッピング元オブジェクトリスト</param>
        /// <returns>マッピング先オブジェクト</returns>
        public static List<Destination> Map(List<Source> SourceLst)
        {
            List<Destination> DestinationLst = new List<Destination>();
            foreach (Source Source in SourceLst)
            {
                var Destination = new Destination();
                Mapping.MappingInfoList.ForEach(x => SetValue(Destination, x.Destination, GetValue(Source, x.Source)));
                DestinationLst.Add(Destination);
            }
            return DestinationLst;
        }

        static private object GetValue(object obj, MappingEntity mapping)
        {
            switch (mapping.MemberType)
            {
                case MemberTypes.Field:
                    return mapping.Field.GetValue(obj);

                case MemberTypes.Property:
                    return mapping.Property.GetValue(obj);
            }
            throw new NotImplementedException("未対応プロパティ");
        }

        static private void SetValue(object obj, MappingEntity mapping, object value)
        {
            switch (mapping.MemberType)
            {
                case MemberTypes.Field:
                    mapping.Field.SetValue(obj, value);
                    break;

                case MemberTypes.Property:
                    mapping.Property.SetValue(obj, value);
                    break;

                default:
                    throw new NotImplementedException("未対応プロパティ");
            }
        }
    }
}