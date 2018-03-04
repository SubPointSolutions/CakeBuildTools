using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SubPointSolutions.DocIndex.Utils;

namespace SubPointSolutions.DocIndex.Data
{
    [DataContract]
    public class DocSample
    {
        public DocSample()
        {
            Tags = new List<TagsValue>();

            IsMethod = true;
            IsClass = false;
        }

        #region properties

        [DataMember]
        public bool IsMethod { get; set; }

        [DataMember]
        public bool IsClass { get; set; }

        [DataMember]
        public List<TagsValue> Tags { get; set; }

        [DataMember]
        public string Scope { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string MethodBodyWithFunction { get; set; }

        [DataMember]
        public string MethodBody { get; set; }

        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string ClassName { get; set; }

        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public string ClassComment { get; set; }

        [DataMember]
        public string MethodFullName { get; set; }

        [DataMember]
        public string ClassFullName { get; set; }

        [DataMember]
        public int MethodParametersCount { get; set; }

        [DataMember]
        public string SourceFileName { get; set; }

        [DataMember]

        public string SourceFileNameWithoutExtension { get; set; }

        [IgnoreDataMember]
        [XmlIgnore]
        public string SourceFileFolder { get; set; }
        [IgnoreDataMember]
        [XmlIgnore]
        public string SourceFilePath { get; set; }

        #endregion

        #region methods

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(MethodBody))
                return MethodBody;

            return base.ToString();
        }

        public static DocSample FromXml(string xml)
        {
            return XmlSerializerUtils.DeserializeFromString<DocSample>(xml);
        }

        public string ToXml()
        {
            return XmlSerializerUtils.SerializeToString(this);
        }

        #endregion
    }
}
