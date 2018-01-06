using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace TPPCore.Service.Common.YamlUtils
{
    public class YamlMappingVisitor : YamlVisitorBase
    {
        public Action<string[],dynamic> ProcessKeyValuePair;

        private Deserializer yamlDeserializer;
        private List<string> keyStack;

        public YamlMappingVisitor()
        {
            yamlDeserializer = new DeserializerBuilder()
                .Build();
            keyStack = new List<string>();
        }

        protected override void VisitPair(YamlNode key, YamlNode value)
        {
            var keyString = deserializeKeyNode(key);
            keyStack.Add(keyString);

            switch (value.NodeType)
            {
                case YamlNodeType.Scalar:
                case YamlNodeType.Sequence:
                    var deserializedValue = deserializeValueNode(value);
                    ProcessKeyValuePair(keyStack.ToArray(), deserializedValue);
                    break;
                default:
                    base.VisitPair(key, value);
                    break;
            }

            keyStack.RemoveAt(keyStack.Count - 1);
        }

        private string deserializeKeyNode(YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Scalar)
            {
                throw new Exception($"Complex mapping keys is not supported. Got {node}");
            }

            var scalar = (YamlScalarNode) node;

            return yamlDeserializer.Deserialize<string>(scalar.Value);
        }

        private object deserializeValueNode(YamlNode node)
        {
            return yamlDeserializer.Deserialize<object>(node.ToString());
        }
    }
}
