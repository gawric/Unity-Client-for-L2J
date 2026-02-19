using UnityEngine;

public class L2WeaponChargeGroup : BaseEffectGroup, IWeaponEffect
{
    private Transform _weaponTransform;
    private Transform _swordTip;

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

    protected override void ApplyShaderParams(Material m, float now, float seed)
    {

       // bool isRing = gameObject.name.Contains("Ring") ||
        //      (transform.parent != null && transform.parent.name.Contains("Ring"));

        m.SetFloat("_StartTime", now);
        m.SetFloat("_Seed", seed);

     
        float initialDelay = m.GetVector("_InitialDelayRange").y;
        float totalLife = _duration + initialDelay;
        m.SetVector("_LifetimeRange", new Vector2(totalLife, totalLife));
        m.SetFloat("_FadeoutStartTime", totalLife * 0.7f);
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
