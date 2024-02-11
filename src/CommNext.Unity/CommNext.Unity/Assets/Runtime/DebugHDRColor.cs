using System;
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
            Debug.Log($"Color1: {color1}, Color2: {color2}");
        }
    }
}