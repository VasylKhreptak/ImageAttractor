using Graphics.UI;
using PoolSystem;
using UnityEngine;

public class ImageAttractorTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private UI_ImageAttractor _imageAttractor;

    [Header("Preferences")]
    [SerializeField] private int _count = 10;
    
    #region MonoBehaiour

    private void OnValidate()
    {
        _imageAttractor ??= FindObjectOfType<UI_ImageAttractor>();
        _canvas ??= FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 screenPoint = Input.mousePosition;

            _imageAttractor.Play(_count, screenPoint, _target, 120);
        }
    }

    #endregion
}
