using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.UI;

public abstract class VignetteController : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 0.1f;
    [SerializeField] private float fadeOutTime = 0.1f;
    [SerializeField] private Image image;

    private readonly UtilityClasses.BaseActionAutomaticTransitionUnscaled _isShowing = new();

    protected virtual void Awake()
    {
        _isShowing.SetFadeTimes(fadeInTime, fadeOutTime);
    }
    
    void Update()
    {
        image.canvasRenderer.SetAlpha(_isShowing.GetFraction(ShouldBeActive()));
    }

    protected abstract bool ShouldBeActive();
}
