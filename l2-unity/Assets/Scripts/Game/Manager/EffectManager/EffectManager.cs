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


        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = new Vector3(0.013f, 0.013f, 0.013f);


        instance.gameObject.SetActive(true);


        instance.Setup(data.settings, castData, target);


        instance.Play();
    }
}