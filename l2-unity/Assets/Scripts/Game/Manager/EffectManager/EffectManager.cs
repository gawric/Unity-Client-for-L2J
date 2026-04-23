using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;
    public EffectDatabase database;
    [SerializeField] private Transform _activeEffectsContainer;

    void Awake() => Instance = this;


    public void PlayEffect(int id, Transform target, MagicCastData castData = null)
    {
        var data = database.effects.Find(e => e.id == id);

        if (data == null || data.prefab == null || _activeEffectsContainer == null)
        {
            Debug.LogWarning($"EffectManager: PlayEffect data == null || data.prefab == null || _activeEffectsContainer == null");
            return;
        }

        BaseEffect instance = Instantiate(data.prefab, target.position, target.rotation, target);

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, target);
        instance.Play();
    }

    public void PlayerImpactEffect(int id, Vector3 point, MagicCastData castData = null)
    {
        var data = database.effects.Find(e => e.id == id);

        if (data == null || data.prefab == null || _activeEffectsContainer == null)
        {
            Debug.LogWarning($"EffectManager: PlayEffect data == null || data.prefab == null || _activeEffectsContainer == null");
            return;
        }

        GameObject dummy = new GameObject("HitPointProxy");
        dummy.transform.position = point;

        BaseEffect instance = Instantiate(data.prefab, point, Quaternion.identity, dummy.transform);

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, dummy.transform);
        instance.Play();

    }
}