namespace R3.Unity.Editor.OdinInspector
{
    /// <summary>
    /// Use this class to change drawing behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To use rendering without OdinInspector, define `DONT_USE_ODIN_INSPECTOR_IN_R3` scripting symbol.
    /// </para>
    /// <para>
    /// <see href="https://docs.unity3d.com/2022.3/Documentation/Manual/CustomScriptingSymbols.html"/>
    /// </para>
    /// </remarks>
    public static class SerializableReactivePropertyDrawingSettings
    {
        /// <summary>
        /// Determines whether to invoke `OnNext` method when the property value changes by Editor.
        /// </summary>
        public static bool NotifyOnValueChanged { get; set; } = true;
    }
}
