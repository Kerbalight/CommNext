using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommNext.Unity.Runtime
{
    public class SampleLine : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public Material lineMaterial;

        private void OnValidate()
        {
            lineRenderer.positionCount = 10;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = lineMaterial;
            // lineRenderer.startColor = Color.green;
            // lineRenderer.endColor = Color.green;
            var connectionGradient = new Gradient();
            connectionGradient.SetKeys(
                new GradientColorKey[] { new(Color.green, 0.0f), new (Color.green, 1.0f) },
                new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f) }
            );
            lineRenderer.colorGradient = connectionGradient;
            
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(10, 10, 10);
            for (var i = 0; i < 10; i++)
            {
                var t = i / 10.0f;
                lineRenderer.SetPosition(i, Vector3.Lerp(start, end, t));
            }
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
