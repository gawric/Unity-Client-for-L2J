using System.Collections.Generic;
using UnityEngine;




public class WindStrikePart : EffectPart
{
    private List<ScaleStep> _scaleSteps;
    private float _currentAlpha = 0;
    private float _elapsedTime = 0;
    private bool _isActive = false;
    private bool _isHiding = false;

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        if (settings is Effect1177Settings wind1177Settings)
        {

            // Базовая инициализация из родительского класса EffectPart
            // (Определяем рендерер и создаем PropertyBlock)
            float baseSize = (partType == EffectPartType.Body) ? wind1177Settings.defaultBodySize : wind1177Settings.defaultFooterSize;
            base.Initialize(settings, baseSize);

            // Присваиваем список шагов анимации
            //_scaleSteps = (partType == EffectPartType.Body) ? wind1177Settings.bodyScales : wind1177Settings.footerScales;

            // 1. Позиционирование (твои офсеты)
            float yOffset = (partType == EffectPartType.Body) ? wind1177Settings.bodyYOffset : wind1177Settings.footerYOffset;
            transform.localPosition = new Vector3(0, yOffset, 0);

            // 2. Рандомные смещения (как в твоем старом коде)
            if (wind1177Settings.useRandomPosition)
            {
                float rZ = Random.Range(wind1177Settings.minZ, wind1177Settings.maxZ);
                float rY = Random.Range(wind1177Settings.minY, wind1177Settings.maxY);
                transform.localPosition += new Vector3(0, rY, rZ);
            }

            // 3. Настройка шейдера (скорость вращения)
            float randSpeed = Random.Range(settings.speedRotateMin, settings.speedRotateMax);
            UpdateShaderFloat(SHADER_PARAMETR_SPEED, randSpeed);
            UpdateShaderFloat(SHADER_PARAMETR_ALPHA, 1); // Начинаем с прозрачного

            // 4. Установка начального масштаба
            transform.localScale = Vector3.one * baseSize;

            _elapsedTime = 0;
            _currentAlpha = 0;
            _isActive = false;
            _isHiding = false;
        }
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

        _elapsedTime += Time.deltaTime;

        // --- ЛОГИКА ПРОЯВЛЕНИЯ (Show/Hide) ---
        if (!_isHiding && _currentAlpha < _settings.stopAlphaShow)
        {
            // Плавное появление до stopAlphaShow
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _settings.stopAlphaShow, Time.deltaTime / _settings.showTime);
            UpdateShaderFloat("_Alpha", _currentAlpha);
        }
        else if (_isHiding)
        {
            // Плавное исчезновение до 0
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0, Time.deltaTime / _settings.hideTime);
            UpdateShaderFloat("_Alpha", _currentAlpha);
            if (_currentAlpha <= 0)
            {
                _isActive = false;
                gameObject.SetActive(false);
            }
        }

        // --- ЛОГИКА МАСШТАБА (Твои ScaleSteps) ---
        UpdateScaleAnimation();
    }

    private void UpdateScaleAnimation()
    {
        if (_scaleSteps == null || _scaleSteps.Count == 0) return;

        // Вычисляем текущий процент времени жизни эффекта
        // (Для простоты берем время от старта, деленное на общую длительность)
        float lifePercent = (_elapsedTime / _settings.defaultLifeTime) * 100f;

        foreach (var step in _scaleSteps)
        {
            // Если мы подошли к временной отметке шага
            if (lifePercent >= step.timePercent)
            {
                float targetSize = _baseSize * (step.scalePercent / 100f);
                Vector3 targetScale = Vector3.one * targetSize;

                // Используем Lerp для плавности, как в твоем коде
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 5f);
            }
        }
    }
}