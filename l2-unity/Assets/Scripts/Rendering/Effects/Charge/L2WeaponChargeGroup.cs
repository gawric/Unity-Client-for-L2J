using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.VFX;

public class L2WeaponChargeGroup : BaseEffectGroup, IWeaponEffect
{
    private Transform _weaponTransform;
    private Transform _swordTip;


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
            vfx.SetFloat("LifetimeRange", _duration);
            vfx.Play();
        }
    }

    private void ApplyMaterialsParams(Renderer renderer, float now, float seed)
    {

        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();


        renderer.GetPropertyBlock(_propBlock);


        Vector4 delayRange = renderer.sharedMaterial.GetVector(InitialDelayRangeID);
        float totalLife = _duration + delayRange.y;


        _propBlock.SetFloat(StartTimeID, now);
        _propBlock.SetFloat(SeedID, seed);
        _propBlock.SetVector(LifetimeRangeID, new Vector2(totalLife, totalLife));
        _propBlock.SetFloat(FadeoutStartTimeID, totalLife * 0.7f);


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
