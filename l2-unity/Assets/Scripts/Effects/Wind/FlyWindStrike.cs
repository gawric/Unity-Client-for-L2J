using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using static S_1177;

public class FlyWindStrike : MonoBehaviour
{
    public Material fadeOutMaterial; // РЎСЃС‹Р»РєР° РЅР° РјР°С‚РµСЂРёР°Р» СЃ РІР°С€РёРј С€РµР№РґРµСЂРѕРј

    private float elapsedTime = 0f;
  
    private float alphaHide = 0;

    private float _minZ = 0.048f;
    private float _maxZ = 0.072f;

    private float _speedRotateMin = 18f;
    private float _speedRotateMax = 23f;

    private bool _isHide = false;
    private bool _isShow = false;
    private Vector3 _originalScale;  

    private float _worldTimeCombo = 0;
    private float _sizeTIme = 0;

    private FlyWindStrike01 _fws01;
    public float height = 0.03f; // РњР°РєСЃРёРјР°Р»СЊРЅР°СЏ РІС‹СЃРѕС‚Р°, РґРѕ РєРѕС‚РѕСЂРѕР№ РїРѕРґРЅРёРјРµС‚СЃСЏ РѕР±СЉРµРєС‚
    public float speed = 0.05f;   // РЎРєРѕСЂРѕСЃС‚СЊ РїРѕРґСЉРµРјР°
    private Vector3 _targetJumpPosition; // Р¦РµР»РµРІР°СЏ РїРѕР·РёС†РёСЏ РґР»СЏ Lerp
    private Vector3 _lastUpdateScale;
    private bool _isRun01 = false;
    private bool _isHide01 = false;

    void Awake()
    {
        GameObject targetObject = this.gameObject;
        //GameObject targetObject = GameObject.Find("windblowin00");
        Renderer renderer = targetObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            fadeOutMaterial = renderer.material;
            fadeOutMaterial.SetFloat("_Alpha", 1);
            _originalScale = transform.localScale;

            _targetJumpPosition = new Vector3(transform.position.x, transform.position.y + height, transform.position.z);
        }
    }

    
    void Update()
    {
        //Debug.Log("World Time Skill " + EffectSkillsmanager.Instance.GetWorldTime());
        //Debug.Log("World Time Skill Percent " + EffectSkillsmanager.Instance.GetPercentCompleteTime());
        elapsedTime += Time.deltaTime;
        UpdateScale(EffectSkillsmanager.Instance.GetPercentCompleteTime() , EffectSkillsmanager.Instance.GetWorldTime() , elapsedTime);
        UpdateSizeWinter(EffectSkillsmanager.Instance.GetPercentCompleteTime(), EffectSkillsmanager.Instance.GetWorldTime(), elapsedTime);


        if (_isHide)
        {
            if (alphaHide <= 0)
            {
                _isHide = false;
                ResetScale();
            }


            float ms = EffectSkillsmanager.Instance.HitTime() / 2;
            float min = (ms / 1000);
            alphaHide = Mathf.Clamp01(0.9f - (elapsedTime / min));
            fadeOutMaterial.SetFloat("_Alpha", alphaHide);
        }

        if (_isShow)
        {
            if(alphaHide >= 0.3f)
            {
                if (!_isRun01)
                {
                    _isRun01 = true;
                    _fws01.SetShow();
                }
            }

            if (alphaHide >= 0.55)
            {
                _isShow = false;
                //_fws01.SetShow();
            }
            _isHide = false;
            float ms = EffectSkillsmanager.Instance.HitTime();
            float min = (ms / 1000);
            alphaHide = Mathf.Clamp01(elapsedTime / min); 
            fadeOutMaterial.SetFloat("_Alpha", alphaHide);

        }
    }

    private void UpdateScale(double percentTime , float worldElapsedTime , float deltaTime)
    {
        Scale[] scaleArr = S_1177.GetScale();
         
        if (scaleArr.Length > 0)
        {
            float time1177_1 = scaleArr[0].TimeProcent;
            float time1177_1_s = scaleArr[0].ScaleSizeProcent;

            if (percentTime  <= time1177_1)
            {
                //var duration = (float)EffectSkillsmanager.Instance.GetPieceCompleteTimeMs(time1177_1_s) / 150;
                float endSize = ConvertProcentToSize(time1177_1_s, defaultSizeXYZ);
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
                    double t1 = (double)0.8f;
                    double t2 = (double)alphaHide;
                    if (t1 >= t2)
                    {
                        SetHide01();
                    }
                    
                }
        
            }
        }

    }

    private void SetHide01()
    {
        if (!_isHide01)
        {
            _isHide01 = true;
            _fws01.SetHide();
        }
       
    }


    private void UpdateSizeWinter(double percentTime, float worldElapsedTime, float deltaTime)
    {

        Scale[] scaleArr = S_1177.GetScale();
        float time1177_2 = scaleArr[1].TimeProcent;


        if (percentTime >= time1177_2 & percentTime <= 100 & _isHide == true)
        {
            var vector = Vector3.Lerp(transform.position, _targetJumpPosition, speed * deltaTime);
            transform.position = vector;
            //Debug.Log(" РњРµРґР»РµРЅРЅРѕ РїРµСЂРµРјРµС‰Р°РµРј РѕР±СЊРµРєС‚ РІРІРµСЂС… " + vector);
        }

    }

    public float ConvertProcentToSize(double percent , float allSize)
    {
        return (float)(percent / 100) * allSize;
    }

    public void ResetScale()
    {
        transform.localScale = new Vector3(S_1177.defaultSizeXYZ, S_1177.defaultSizeXYZ, S_1177.defaultSizeXYZ + 1.5f);
    }
    public void ResetPosition()
    {
        //transform.position = new Vector3(S_1177.defaultSizeXYZ, S_1177.defaultSizeXYZ, S_1177.defaultSizeXYZ + 1.5f);
    }
    public void UpdateWorldTime(float timeCombo , float sizeTime)
    {
        _worldTimeCombo = timeCombo;
        _sizeTIme = sizeTime;
    }
    private void StartLocalPositionRandom()
    {
        float randomZ = UnityEngine.Random.Range(_minZ, _maxZ);
        transform.localPosition = new Vector3(0, 0, randomZ);
    }

    private void StartLocalSpeedRandom()
    {
        float randomZ = UnityEngine.Random.Range(_speedRotateMin, _speedRotateMax);
        fadeOutMaterial.SetFloat("_Speed", randomZ);
    }

    public void StartScalePosition()
    {
        float new_z = GetScalePercent(_originalScale);
        transform.localScale = new Vector3(_originalScale.x , _originalScale.y , new_z);
    }

    private float GetScalePercent(Vector3 localScale)
    {
        //Vector3 scale = transform.localScale;
        double start = S_1177.startSize;
        float result = (float)(start / 100) * localScale.z;
        return result;
    }

    public void SetFlS01(FlyWindStrike01 fws01)
    {
        this._fws01 = fws01;
    }
    public void SetShow()
    {
        _isShow = true;
        _isHide = false;
        _isHide01 = false;
        _isRun01 = false;
        alphaHide = 0;
        elapsedTime = 0;
        StartLocalPositionRandom();
        StartLocalSpeedRandom();
        StartScalePosition();
        _targetJumpPosition = new Vector3(transform.position.x, transform.position.y + height, transform.position.z);
        _isRun01 = false;
    }

    public void SetHide()
    {
        _isHide = true;
        _isShow = false;
        alphaHide = 0.8f;
        elapsedTime = 0;
    }

}
