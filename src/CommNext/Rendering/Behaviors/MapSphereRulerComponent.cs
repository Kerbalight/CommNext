using KSP.Map;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

public class MapSphereRulerComponent : MonoBehaviour
{
    private double _range;
    private MeshRenderer _meshRenderer = null!;
    private Color? _lastColor;
    private MaterialPropertyBlock _propertyBlock = null!;

    private static readonly int ColorID = Shader.PropertyToID("_Color");

    public void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
    }

    public void Configure(double range, Color? color)
    {
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _meshRenderer.SetPropertyBlock(_propertyBlock);
        if (color.HasValue)
        {
            _lastColor = color;
            _propertyBlock.SetColor(ColorID, color.Value);
        }

        _range = range;
    }

    public void SetColor(Color color)
    {
        if (_lastColor == color) return;
        _lastColor = color;
        _propertyBlock.SetColor(ColorID, color);
        _meshRenderer.SetPropertyBlock(_propertyBlock); // TODO Needed?
    }

    private void Update()
    {
        var radius = (float)(_range / ConnectionsRenderer.Instance.GetMap3dScaleInv());
        transform.localScale = new Vector3(radius, radius, radius);
    }
}