using UnityEngine;
using UnityEngine.UI;

public class ProjectedImage : MonoBehaviour
{
    [SerializeField] private GameObject imageObject;
    [SerializeField] private float smoothSpeed = 15f;   
    
    public Vector3 WorldTarget { get; set; }=Vector3.zero;

    public void SetShowing(bool val)
    {
        isShowing = val;
    }
    
    private RectTransform _imageRect;
    private Vector2 _targetCanvasPosition;
    private bool wasActive = false;
    private bool isShowing = false;

    private void Awake()
    {
        _imageRect = imageObject.GetComponent<RectTransform>();
    }

    private void Start()
    {
        imageObject.SetActive(false);
    }

    private void Update()
    {

        if (!isShowing)
        {
            wasActive = false;
            imageObject.SetActive(false);
            return;
        }
        
        var screenPos = GlobalCameraManager.PlayerCamera.WorldToScreenPoint(WorldTarget);
        if (screenPos.z <= 0)
        {
            wasActive = false;
            imageObject.SetActive(false);
            return;
        }

        _targetCanvasPosition = screenPos;

        
        if (!wasActive)
        {
            wasActive = true;
            imageObject.SetActive(true);
            _imageRect.anchoredPosition = _targetCanvasPosition;
        }
        else
        {
            _imageRect.anchoredPosition = Vector2.Lerp(
                _imageRect.anchoredPosition,
                _targetCanvasPosition,
                Time.unscaledDeltaTime * smoothSpeed
            );
        }
    }

    
}
