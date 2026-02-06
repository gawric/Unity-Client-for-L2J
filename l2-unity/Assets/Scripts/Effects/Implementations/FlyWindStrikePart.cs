using UnityEngine;
using System.Collections.Generic;

public class FlyWindStrikePart : EffectPart
{
    public EffectPart linkedPart; // —сылка на FlyWindStrike01 (если есть в префабе)

    private bool _isActive = false;
    private bool _isHiding = false;


    private Vector3 _startPosition;
    private Vector3 _targetJumpPosition;
    private float _shotTime = 0;
    public override void Setup(EffectSettings settings , MagicCastData castData)
    {

        base.Initialize(settings, settings.defaultBodySize);
        _castData = castData;
        _shotTime = castData.GetShotTimeNormalize();


        float rZ = Random.Range(0.048f, 0.072f);
        float rY = Random.Range(0.19f, 0.2f);
        //transform.localPosition = new Vector3(0, settings.footerYOffset, rZ);
        transform.localPosition = new Vector3(0, rY, rZ);
        _startPosition = transform.position;
        _targetJumpPosition = _startPosition + Vector3.up * 1;

       
        float randSpeed = Random.Range(19f, 22f);
        UpdateShaderFloat(SHADER_PARAMETR_SPEED, randSpeed);
        UpdateShaderFloat(SHADER_PARAMETR_MAX_ALPHA, settings.maxAlphaFooter);

        UpdateShaderFloat(SHADER_PARAMETR_SCALE_START_SIZE, settings.scaleSizeStart);
        UpdateShaderFloat(SHADER_PARAMETR_SCALE_END_SIZE, settings.scaleSizeEnd);
        UpdateShaderFloat(SHADER_PARAMETR_SCALE_START_TIME, settings.startTimeScale);
        UpdateShaderFloat(SHADER_PARAMETR_SCALE_END_TIME, settings.endTimeScale);

        UpdateShaderFloat(SHADER_PARAMETR_START_ALPHA_TIMEV1, settings.startAlphaTimeV1);
        UpdateShaderFloat(SHADER_PARAMETR_END_ALPHA_TIMEV1, settings.endAlphaTimeV1);
        UpdateShaderFloat(SHADER_PARAMETR_START_ALPHA_TIMEV0, settings.startAlphaTimeV0);
        UpdateShaderFloat(SHADER_PARAMETR_END_ALPHA_TIMEV0, settings.endAlphaTimeV0);

    }

    public override void PlayPart()
    {
        _isActive = true;
        _isHiding = false;
        gameObject.SetActive(true);
    }

    public override void StopPart()
    {
        _isHiding = true;
    }

    void Update()
    {
        
        if (!_isActive) return;
         float elapsed = Time.time - _castData.StartTime;
         float progress = Mathf.Clamp01(elapsed / _castData.HitTime);

        Debug.Log("[FlyWindStrikePart Load ] > _AnimProgress " + progress);
        UpdateShaderFloat("_AnimProgress", progress);

        if (progress >= _shotTime)
        {
            transform.position = Vector3.Lerp(transform.position, _targetJumpPosition, settings.flyJumpSpeed * Time.deltaTime);
        }

        if (progress >= 1f)
        {
            _isActive = false;
        }
    }
}
