using UnityEngine;
using static S_1177;

public class BodyWindStrike : MonoBehaviour
{
    public Material _fadeOutMaterial;
    private Vector3 _originalScale;
   
    public float height = 0.03f; // Максимальная высота, до которой поднимется объект
    private float _speedRotateMin = 23f;
    private float _speedRotateMax = 27f;
    private Vector3 _lastUpdateScale;

    private bool _isHide = false;
    private bool _isShow = false;

    private bool _isRandom = false;
    //default size scale 6
    private float _alphaHide = 0;
    //default 0.3
    public float _stopAlphaShow = 0;
    private float _elapsedTime = 0f;

    public float _maxAlpha = 0f;
    //default 3
    public float _timeElapsedShow = 0f;
    //default 2
    private float _timeElapsedHide = 0f;

    private float _minZ = 0f;
    private float _maxZ = -0.02f;

    private float _minY = 0.00f;
    private float _maxY = -0.04f;

    private ShimmerEffects _shimmerEffects;

    void Awake()
    {
        GameObject targetObject = this.gameObject;
        Renderer renderer = targetObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            _fadeOutMaterial = renderer.material;
            _fadeOutMaterial.SetFloat("_Alpha", 1);
            _originalScale = transform.localScale;
            if (_shimmerEffects == null) _shimmerEffects = new ShimmerEffects();


            _shimmerEffects.SetMaterial(_fadeOutMaterial);
        }
    }

    public void SetElapsedHide(float hideTime)
    {
        _timeElapsedHide = hideTime;
    }

   
    void Update()
    {
        _elapsedTime += Time.deltaTime;
        UpdateScale(EffectSkillsmanager.Instance.GetPercentCompleteTime(), EffectSkillsmanager.Instance.GetWorldTime(), _elapsedTime);
        // UpdateScale(_elapsedTime, _isHide);
        if (_isShow)
        {
            if (_alphaHide >= _stopAlphaShow) _isShow = false;
            _shimmerEffects.ShowElement(_elapsedTime, _timeElapsedShow, ref _alphaHide);
        }

        if (_isHide)
        {

            if (_alphaHide <= 0) _isHide = false;
            _shimmerEffects.HideElement(_elapsedTime, _timeElapsedHide, ref _alphaHide, _maxAlpha);

        }
    }


    public void SetShow(bool isRandom)
    {
        _alphaHide = 0;
        _elapsedTime = 0;
        _isShow = true;
        _isRandom = isRandom;
        if (isRandom)
        {
            StartLocalPositionRandom();
            StartLocalSpeedRandom();
            StartScalePosition();
        }
    
    }

    public void SetHide()
    {
        _isHide = true;
        _isShow = false;
        _alphaHide = _maxAlpha;
        _elapsedTime = 0;
    }

    private void StartLocalPositionRandom()
    {
        float randomZ = UnityEngine.Random.Range(_minZ, _maxZ);
        float randomY = UnityEngine.Random.Range(_minY, _maxY);
        transform.localPosition = new Vector3(0, randomY, randomZ);
    }

    private void StartLocalSpeedRandom()
    {
        float randomZ = Random.Range(_speedRotateMin, _speedRotateMax);
        _fadeOutMaterial.SetFloat("_Speed", randomZ);
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




    //experiment

    private void UpdateScale(double percentTime, float worldElapsedTime, float deltaTime)
    {
        Scale[] scaleArr = S_1177.GetScaleBody();
        if (!_isRandom) return;
        if (scaleArr.Length > 0)
        {
            float time1177_1 = scaleArr[0].TimeProcent;
            float time1177_1_s = scaleArr[0].ScaleSizeProcent;

            if (percentTime <= time1177_1)
            {
                //var duration = (float)EffectSkillsmanager.Instance.GetPieceCompleteTimeMs(time1177_1_s) / 150;
                float endSize = ConvertProcentToSize(time1177_1_s, defaultBodySizeXYZ);
                Vector3 targetScale = new Vector3(endSize, endSize, endSize);

                float ms = EffectSkillsmanager.Instance.HitTime() / 14;
                float min = (ms / 1000);
                float t = Mathf.Clamp01(deltaTime / min); // 

                var z = Vector3.Lerp(_originalScale, targetScale, t);

                transform.localScale = z;
                _lastUpdateScale = z;

            }
            else
            {
                //start time 80%
                //end size 20%
                float time1177_2 = scaleArr[1].TimeProcent;
                float procent_end_scale = scaleArr[1].ScaleSizeProcent;

                if (percentTime >= time1177_2 & percentTime <= 100 & _isHide == true)
                {
                    //convert second (/ 150)
                    var duration = (float)EffectSkillsmanager.Instance.GetPieceCompleteTimeMs(procent_end_scale) / 150;

                    float endSize = ConvertProcentToSize(procent_end_scale, _lastUpdateScale.z);

                    float ms = EffectSkillsmanager.Instance.HitTime() / 2;
                    float min = (ms / 1000);


                    float t = Mathf.Clamp01(deltaTime / min); // 
                    Vector3 targetScale = new Vector3(endSize, endSize, endSize);


                    var z = Vector3.Lerp(_lastUpdateScale, targetScale, t);
                    transform.localScale = z;
                    //  float epsilon = 0.0001f;
                    double t1 = (double)0.4f;
                   // double t2 = (double)alphaHide;
                    //if (t1 >= t2)
                   // {
                   //     SetHide01();
                   // }

                }

            }
        }

    }

    public float ConvertProcentToSize(double percent, float allSize)
    {
        return (float)(percent / 100) * allSize;
    }
}
