
using UnityEngine;
using UnityEngine.VFX;

public class L2WeaponChargeGroup : BaseEffectGroup, IWeaponEffect
{
    private Transform _weaponTransform;
    private Transform _swordTip;
    private float adjustedLife;
    private const float ReferenceWeaponLength = 0.3550376f;
    private float _timeMultiplier = 1;
    private static readonly int StartTimeID = Shader.PropertyToID("_StartTime");
    private static readonly int SeedID = Shader.PropertyToID("_Seed");
    private static readonly int LifetimeRangeID = Shader.PropertyToID("_LifetimeRange");
    private static readonly int FadeoutStartTimeID = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int InitialDelayRangeID = Shader.PropertyToID("_InitialDelayRange");


    private MaterialPropertyBlock _propBlock;

    public void SetWeapon(Transform weaponTransform)
    {
        _weaponTransform = weaponTransform;

        if (_weaponTransform != null)
        {
            // Ищем объект Sword_Tip в дочерних объектах оружия
            _swordTip = weaponTransform.Find("Sword_Tip");
            float currentLength = Vector3.Distance(_weaponTransform.position, _swordTip.position);
            
            
            if (_swordTip == null)
            {
                // Если не нашли по имени, пробуем найти в глубине (на случай сложной иерархии)
                foreach (Transform child in weaponTransform.GetComponentsInChildren<Transform>())
                {
                    if (child.name == "Sword_Tip")
                    {
                        _swordTip = child;
                        break;
                    }
                }
            }

           float maxMultiplierFor2Meters = 2.0f / ReferenceWeaponLength;
           _timeMultiplier= Mathf.Min(maxMultiplierFor2Meters, currentLength / ReferenceWeaponLength);
           
           // Если оружие короче эталонного (timeMultiplier < 1), 
           // используем квадратичное уменьшение, чтобы эффект быстрее проигрывался на коротком оружии
           if (_timeMultiplier < 1.0f)
           {
               _timeMultiplier = _timeMultiplier * _timeMultiplier;
           }
           
           // Ограничиваем итоговое время жизни снизу нашим порогом minLifeTime
           adjustedLife = Mathf.Max(minLifeTime, _duration * _timeMultiplier);
           Debug.Log($"_swordTip размер меча: {currentLength} , {_timeMultiplier} , {adjustedLife}");
        }
    }

    protected override void ApplyShaderParams(Component component, float now, float seed)
    {
        if (component == null) return;

        if (component is Renderer renderer)
        {
            ApplyMaterialsParams(renderer, now, seed);
        }
        else if (component is VisualEffect vfx)
        {
            Debug.Log($"VFX USE COMPONENT!: {_duration}");
            
            // Защита от деления на ноль
            float safeDuration = Mathf.Max(0.001f, _duration);
            
            if (useFixedLifetime)
            {
                Debug.Log($"_swordTip размер меча fx1: {_duration}");
                TrySetVfxFloat(vfx, "LifetimeRange", _duration);
                TrySetVfxFloat(vfx, "_LifetimeRange", _duration);
                vfx.Play();
            }
            else
            {
                // Компенсируем закрутку (ForwardOffset): чтобы шаг спирали выглядел одинаково,
                // на длинном оружии нужно больше витков закрутки, а на коротком - меньше.
                // Допустим, 3.5f - это базовая закрутка для эталонного оружия.
                float baseForwardOffset = 3.5f;
                // Для короткого оружия ограничиваем снизу, чтобы витков не было слишком мало
                float forwardOffset = Mathf.Max(3.5f, baseForwardOffset * _timeMultiplier);
                
                // --- СПЕЦИАЛЬНОЕ УСЛОВИЕ ДЛЯ КОРОТКОГО ОРУЖИЯ ---
                // Если оружие короче эталонного пока отключаем смотриться странно Glow пролетает быстро, а этот эффект задерживается
                //if (_timeMultiplier < 1.0f)
                //{
                 //   adjustedLife = 1.5f;       
                //    forwardOffset = 1.3f;      
                 //   float turnsPerSecond = 0.5f;
                 //   vfx.SetFloat("TurnsPerSecond", turnsPerSecond);
                //}
                
                TrySetVfxFloat(vfx, "ForwardOffset", forwardOffset);
                TrySetVfxFloat(vfx, "LifetimeRange", adjustedLife);
                TrySetVfxFloat(vfx, "_LifetimeRange", adjustedLife);
                Debug.Log($"_swordTip размер меча fx2: {adjustedLife} , {forwardOffset}");
                vfx.Play();
            }
 
        }
    }

    private void TrySetVfxFloat(VisualEffect vfx, string propertyName, float value)
    {
        if (vfx == null || string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        if (vfx.HasFloat(propertyName))
        {
            vfx.SetFloat(propertyName, value);
        }
    }

    private void ApplyMaterialsParams(Renderer renderer, float now, float seed)
    {

        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();


        renderer.GetPropertyBlock(_propBlock);


        Vector4 delayRange = renderer.sharedMaterial.GetVector(InitialDelayRangeID);
        float totalLife = _duration + delayRange.y;

        if (useFixedLifetime)
        {
            _propBlock.SetFloat(StartTimeID, now);
            _propBlock.SetFloat(SeedID, seed);
            _propBlock.SetVector(LifetimeRangeID, new Vector2(_duration, _duration));
            _propBlock.SetFloat(FadeoutStartTimeID, _duration * 0.7f);
            Debug.Log($"ApplyMaterialsParams fixed: {now}, {seed}, {_duration}, {totalLife * 0.7f}");
        }
        else
        {
            _propBlock.SetFloat(StartTimeID, now);
            _propBlock.SetFloat(SeedID, seed);
            _propBlock.SetVector(LifetimeRangeID, new Vector2(adjustedLife, adjustedLife));
            _propBlock.SetFloat(FadeoutStartTimeID, totalLife * 0.7f);
            Debug.Log($"ApplyMaterialsParams calc: {now}, {seed}, {adjustedLife}, {totalLife * 0.7f}");
        }


        renderer.SetPropertyBlock(_propBlock);
    }


    private void OnDrawGizmos()
    {
        if (_weaponTransform == null || _swordTip == null) return;

        // Линия вдоль лезвия (Синяя)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_weaponTransform.position, _swordTip.position);

        // Сфера в основании (Зеленая)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_weaponTransform.position, 0.02f);

        // Сфера на кончике (Красная)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_swordTip.position, 0.02f);
    }
}
