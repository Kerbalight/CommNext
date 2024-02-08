using KSP.Map;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Rendering.Behaviors
{
    public class RulerSphereComponent : MonoBehaviour
    {
        private double _range;
        
        private bool _isTracking;

        private MeshRenderer _meshRenderer;
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        private Map3DFocusItem _target;
        private Action<RulerSphereComponent> _onMissingNode;

        public void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("Map");
        }
        
        public void Track(Map3DFocusItem target, double range, Color? color, Action<RulerSphereComponent> onMissingNode)
        {
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (color.HasValue)
            {
                _meshRenderer.material.SetColor(ColorID, color.Value);
            }

            _range = range;
            _target = target;
            _onMissingNode = onMissingNode;
            _isTracking = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_isTracking) return;
            if (_target == null)
            {
                _onMissingNode?.Invoke(this);
                return;    
            }
            
            var radius = (float)(_range / ConnectionsRenderer.Instance.GetMap3dScaleInv());
            var currentTransform = transform;
            currentTransform.localScale = new Vector3(radius, radius, radius);
            currentTransform.position = _target.transform.position;
        }
    }
}
