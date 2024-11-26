using System;
using UnityEngine;

namespace R3
{
    [Serializable]
    public class SerializableReactiveProperty<T> : ReactiveProperty<T>, ISerializationCallbackReceiver
    {
        [SerializeField]
        T value;

        public SerializableReactiveProperty()
            : base(default!)
        {
        }

        public SerializableReactiveProperty(T value)
            : base(value)
        {
        }

        protected override void OnValueChanged(T value)
        {
            this.value = value;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            GetValueRef() = this.value; // force set
        }
    }
}
