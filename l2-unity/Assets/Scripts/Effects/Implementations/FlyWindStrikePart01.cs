using UnityEngine;
using System.Collections.Generic;

public class FlyWindStrikePart01 : EffectPart
{

    private bool _isActive = false;



    private Vector3 _startPosition;
    private Vector3 _targetJumpPosition;
    private float _shotTime = 0;
    private float _dynamicEndSize = 14.5f;
    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        if (settings is Effect1177Settings flyWind1177Settings)
        {
            base.Initialize(flyWind1177Settings, flyWind1177Settings.defaultBodySize);
            _castData = castData;
            _shotTime = castData.GetShotTimeNormalize();


            float rZ = Random.Range(0.048f, 0.072f);
            float rY = Random.Range(0.19f, 0.2f);
            transform.localPosition = new Vector3(0, rY, rZ);
            _startPosition = transform.position;
            _targetJumpPosition = _startPosition + Vector3.up * 1;


            float randSpeed = Random.Range(19f, 22f);
            UpdateShaderFloat(SHADER_PARAMETR_SPEED, randSpeed);
            UpdateShaderFloat(SHADER_PARAMETR_MAX_ALPHA, settings.maxAlphaFooter);

            UpdateShaderFloat(SHADER_PARAMETR_SCALE_START_SIZE, 22);
            UpdateShaderFloat(SHADER_PARAMETR_SCALE_END_SIZE, 14.5f);
            UpdateShaderFloat(SHADER_PARAMETR_SCALE_START_TIME, 0.6f);
            UpdateShaderFloat(SHADER_PARAMETR_SCALE_END_TIME, flyWind1177Settings.endTimeScale);

            UpdateShaderFloat(SHADER_PARAMETR_START_ALPHA_TIMEV1, flyWind1177Settings.startAlphaTimeV1);
            UpdateShaderFloat(SHADER_PARAMETR_END_ALPHA_TIMEV1, flyWind1177Settings.endAlphaTimeV1);
            UpdateShaderFloat(SHADER_PARAMETR_START_ALPHA_TIMEV0, flyWind1177Settings.startAlphaTimeV0);
            UpdateShaderFloat(SHADER_PARAMETR_END_ALPHA_TIMEV0, flyWind1177Settings.endAlphaTimeV0);
        }
    }

    public override void PlayPart()
    {
        _isActive = true;
 
        gameObject.SetActive(true);
    }

    public override void StopPart()
    {
    }

    void Update()
    {

        if (!_isActive) return;
        float elapsed = Time.time - _castData.StartTime;
        float progress = Mathf.Clamp01(elapsed / _castData.HitTime);


        UpdateShaderFloat("_AnimProgress", progress);

  
        float target = 22;
        if (progress >= 0.9f) target = 19;
        else if (progress >= 0.8f) target = 20;
        else if (progress >= 0.6f) target = 21;

        // Плавно приближаем текущий размер к цели (скорость 50 единиц в секунду)
        _dynamicEndSize = Mathf.MoveTowards(_dynamicEndSize, target, 50f * Time.deltaTime);
        Debug.Log("[FlyWindStrikePart01 Load ] > _AnimProgress _dynamicEndSize > " + _dynamicEndSize);
        UpdateShaderFloat(SHADER_PARAMETR_SCALE_END_SIZE, _dynamicEndSize);


        // 3. ЛЕГКОЕ ПЕРЕМЕЩЕНИЕ ВВЕРХ-ВНИЗ (Bobbing effect)
        float bobbingOffset = Mathf.Sin(Time.time * 1f) * 0.02f;

        transform.position = new Vector3(
            _startPosition.x,
            _startPosition.y + bobbingOffset,
            _startPosition.z
        );

        if (progress >= 1f)
        {
            _isActive = false;
        }
    }
}
