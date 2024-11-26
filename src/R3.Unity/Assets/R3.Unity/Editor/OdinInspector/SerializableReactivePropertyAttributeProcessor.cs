using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Scripting;

namespace R3.Unity.Editor.OdinInspector
{
    [Preserve]
    internal class SerializableReactivePropertyAttributeProcessor<TProperty, TValue> : OdinAttributeProcessor<TProperty> where TProperty : SerializableReactiveProperty<TValue>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) =>
            attributes.Add(new InlinePropertyAttribute());
    }
}
