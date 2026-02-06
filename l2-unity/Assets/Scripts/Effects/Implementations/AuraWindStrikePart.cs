using UnityEngine;

public class AuraWindStrikePart : EffectPart
{


    private float _currentAlpha = 0;
    private float _elapsedTime = 0;
    private bool _isActive = false;
    private bool _isHiding = false;

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        // Инициализируем базу (рендерер, пропблок)
        base.Initialize(settings, settings.auraDefaultSize);

        // 1. Установка позиции (Твоя логика GetGround)
        // В L2 аура чуть выше футера, чтобы не мерцать
        transform.localPosition = new Vector3(0, settings.auraYOffset, 0);

        // 2. Начальный масштаб и прозрачность
        transform.localScale = Vector3.one * settings.auraDefaultSize;
        UpdateShaderFloat(SHADER_PARAMETR_ALPHA, 0);

        _elapsedTime = 0;
        _currentAlpha = 0;
    }

    public override void PlayPart()
    {
        _isActive = true;
        _isHiding = false;
        _elapsedTime = 0;
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

        // Плавное появление (Твои 3 секунды до 0.3f)
        if (!_isHiding && _currentAlpha < 0.3f)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0.3f, Time.deltaTime / 3f);
            UpdateShaderFloat(SHADER_PARAMETR_ALPHA, _currentAlpha);
        }

        // Плавное исчезновение (Твои 4 секунды)
        if (_isHiding)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0, Time.deltaTime / 4f);
            //UpdateShaderFloat(SHADER_PARAMETR_ALPHA, _currentAlpha);

            // Твоя специфическая логика скейла при скрытии ауры
            // UpdateScale(elapsedTime, new Scale(50, 30))
            float targetSize = baseSize * 0.3f; // 30% от размера
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetSize, Time.deltaTime);

            if (_currentAlpha <= 0)
            {
                _isActive = false;
                gameObject.SetActive(false);
            }
        }
    }
}
