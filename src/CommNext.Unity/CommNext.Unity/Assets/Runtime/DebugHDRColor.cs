using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

namespace CommNext.Unity.Editor
{
    public class DebugHDRColor : MonoBehaviour
    {
        [ColorUsage(true, true)]
        [SerializeField]
        public Color color1;

        [ColorUsage(true, true)]
        [SerializeField]
        public Color color2;

        public void OnValidate()
        {
            Debug.Log(
                $"Color1: new Color({color1.r.ToString(CultureInfo.InvariantCulture)}f, {color1.g.ToString(CultureInfo.InvariantCulture)}f, {color1.b.ToString(CultureInfo.InvariantCulture)}f, {color1.a.ToString(CultureInfo.InvariantCulture)}f);\n" +
                $"Color2: new Color({color2.r.ToString(CultureInfo.InvariantCulture)}f, {color2.g.ToString(CultureInfo.InvariantCulture)}f, {color2.b.ToString(CultureInfo.InvariantCulture)}f, {color2.a.ToString(CultureInfo.InvariantCulture)}f);");
        }
    }
}