using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Scripting;

namespace R3.Unity.Editor.OdinInspector
{
    [Preserve]
    internal class SerializableReactivePropertyPropertyProcessor<TProperty, TValue> : OdinPropertyProcessor<TProperty> where TProperty : SerializableReactiveProperty<TValue>
    {
        private const string ValueFieldName = "value";

        public override void ProcessMemberProperties(List<InspectorPropertyInfo> infos)
        {
            InspectorPropertyInfo valueFieldInfo = infos.Find(info => info is { PropertyName: ValueFieldName });
            List<Attribute> valueFieldAttributes = valueFieldInfo?.GetEditableAttributesList();
            if (valueFieldAttributes == null) return;

            valueFieldAttributes.Add(new HideLabelAttribute());

            if (SerializableReactivePropertyDrawingSettings.NotifyOnValueChanged)
                valueFieldAttributes.Add(new OnValueChangedAttribute(nameof(ReactiveProperty<TValue>.OnNext), includeChildren: true));
        }
    }
}
