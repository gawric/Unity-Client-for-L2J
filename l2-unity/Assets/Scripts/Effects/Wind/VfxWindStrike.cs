using UnityEngine;
using UnityEngine.VFX;

public class VfxWindStrike : MonoBehaviour
{
    public VisualEffect _fadeOutMaterial;


    private bool _isHide = false;
    private bool _isShow = false;

    private float _alphaHide = 0;
    private float _elapsedTime = 0f;

    public float _timeElapsedShow = 0f;
    public float _timeElapsedHide = 0f;

    public float _maxAlpha = 0f;



    private ShimmerEffects _shimmerEffects;

    void Awake()
    {
        GameObject targetObject = this.gameObject;

        VisualEffect vfx = targetObject.GetComponent<VisualEffect>();

        if (vfx != null)
        {
            _fadeOutMaterial = vfx;
            _fadeOutMaterial.SetFloat("_Alpha", 0);

            if (_shimmerEffects == null) _shimmerEffects = new ShimmerEffects();
            _shimmerEffects.SetVfx(_fadeOutMaterial);
        }
    }




    void Update()
    {
        _elapsedTime += Time.deltaTime;

        // UpdateScale(_elapsedTime, _isHide);
        if (_isShow)
        {
            if (_alphaHide >= 1f) _isShow = false;
            _shimmerEffects.ShowElement(_elapsedTime, _timeElapsedShow, ref _alphaHide);
        }

        if (_isHide)
        {

            if (_alphaHide <= 0) _isHide = false;
            _shimmerEffects.HideElement(_elapsedTime, _timeElapsedHide, ref _alphaHide, _maxAlpha);

        }
    }


    public void SetShow()
    {
        _alphaHide = 0;
        _elapsedTime = 0;
        _isShow = true;
    }

    public void SetHide()
    {
        _isHide = true;
        _isShow = false;
        _alphaHide = 1f;
        _elapsedTime = 0;
    }

 
}
