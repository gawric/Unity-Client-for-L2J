using System.ComponentModel;
using UnityEngine;
using static S_1177;

public class FlyWindStrike01 : MonoBehaviour
{
    public Material _fadeOutMaterial;
    private ShimmerEffects _shimmerEffects;
    private float _alphaHide = 0;
    private float _elapsedTime = 0f;
    private bool _isHide = false;
    private bool _isShow = false;
    private Vector3 _originalScale;
    private float _speedRotateMin = 18f;
    private float _speedRotateMax = 23f;
    private float _minZ = 0.048f;
    private float _maxZ = 0.072f;
    void Awake()
    {
        GameObject targetObject = this.gameObject;
        //GameObject targetObject = GameObject.Find("windblowin00");
        Renderer renderer = targetObject.GetComponent<Renderer>();

        if (_shimmerEffects == null) _shimmerEffects = new ShimmerEffects();

        if (renderer != null)
        {
            _fadeOutMaterial = renderer.material;
            _fadeOutMaterial.SetFloat("_Alpha", 0);
            _originalScale = transform.localScale;
        }
        _shimmerEffects.SetMaterial(_fadeOutMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        _elapsedTime += Time.deltaTime;

        UpdateScale(_elapsedTime, _isHide);
        if (_isShow)
        {
            if (_alphaHide >= 0.3f) _isShow = false;
            _shimmerEffects.ShowElement(_elapsedTime, 3f, ref _alphaHide);
        }

        if (_isHide)
        {

            if (_alphaHide <= 0) _isHide = false;
            _shimmerEffects.HideElement(_elapsedTime, 2f, ref _alphaHide, 0.6f);

        }
    }

    private void UpdateScale(float elapsedTime, bool isHide)
    {
        if (isHide == true)
        {
            Scale[] scaleArr = S_1177.GetScale();
            _shimmerEffects.UpdateScaleStraightAway(EffectSkillsmanager.Instance.GetPercentCompleteTime(), elapsedTime, scaleArr[1], S_1177.defaultSizeXYZ, transform, 1000);
        }

    }
    public void SetShow()
    {
        _alphaHide = 0;
        _elapsedTime = 0;
        _isShow = true;
        StartLocalPositionRandom();
        StartLocalSpeedRandom();
        StartScalePosition();
    }

    private void StartLocalSpeedRandom()
    {
        float randomZ = Random.Range(_speedRotateMin, _speedRotateMax);
        _fadeOutMaterial.SetFloat("_Speed", randomZ);
    }

    public void SetHide()
    {
        _isHide = true;
        _isShow = false;
        _alphaHide = 0.6f;
        _elapsedTime = 0;
    }

    public void StartScalePosition()
    {
        float new_z = GetScalePercent(_originalScale);
        transform.localScale = new Vector3(_originalScale.x, _originalScale.y, new_z);
    }

    private float GetScalePercent(Vector3 localScale)
    {
        //Vector3 scale = transform.localScale;
        double start = S_1177.startSize;
        float result = (float)(start / 100) * localScale.z;
        return result;
    }
    private void StartLocalPositionRandom()
    {
        float randomZ = UnityEngine.Random.Range(_minZ, _maxZ);
        transform.localPosition = new Vector3(0, 0, randomZ);
    }
}
