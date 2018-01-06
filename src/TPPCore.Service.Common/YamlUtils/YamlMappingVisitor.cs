using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace TPPCore.Service.Common.YamlUtils
{
    public class YamlMappingVisitor : YamlVisitorBase
    {
        public Action<string[],YamlNode> ProcessKeyValuePair;

        private List<string> keyStack;

        public YamlMappingVisitor()
        {
            keyStack = new List<string>();
        }

        protected override void VisitPair(YamlNode key, YamlNode value)
        {
            var keyString = getKeyNodeString(key);
            keyStack.Add(keyString);

            ProcessKeyValuePair(keyStack.ToArray(), value);

            switch (value.NodeType)
            {
                case YamlNodeType.Scalar:
                case YamlNodeType.Sequence:
                    break;
                default:
                    base.VisitPair(key, value);
                    break;
            }

            keyStack.RemoveAt(keyStack.Count - 1);
        }

        private string getKeyNodeString(YamlNode node)
        {
            if (node.NodeType != YamlNodeType.Scalar)
            {
                throw new Exception($"Complex mapping keys is not supported. Got {node}");
            }

            var scalar = (YamlScalarNode) node;

            return scalar.Value;
        }
    }
}
