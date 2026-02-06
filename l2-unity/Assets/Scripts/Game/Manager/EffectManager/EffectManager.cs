using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;
    public EffectDatabase database;
    [SerializeField] private Transform _activeEffectsContainer;

    void Awake() { Instance = this; }


    public void PlayEffect(int id, Transform target , MagicCastData castData)
    {
        var prefab = database.GetPrefab(id);
        var data = database.effects.Find(e => e.id == id);

        if (data != null && data.prefab != null && _activeEffectsContainer != null)
        {

            BaseEffect instance = Instantiate(data.prefab, target.position, target.rotation, _activeEffectsContainer);

            //instance.transform.localPosition = Vector3.zero;
            instance.gameObject.SetActive(true);

            if (instance is DefaultEffect defaultEffect)
            {
                defaultEffect.Setup(data.settings , castData);
            }


            instance.Play();
        }
    }

}