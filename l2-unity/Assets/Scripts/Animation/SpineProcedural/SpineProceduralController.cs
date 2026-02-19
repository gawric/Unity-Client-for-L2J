using System.Collections.Generic;
using UnityEngine;

public class SpineProceduralController : MonoBehaviour
{
    private Dictionary<Transform, BoneModification> _activeModifications = new Dictionary<Transform, BoneModification>();
    private List<Transform> _bonesToRemove = new();
    public float fadeSpeed = 10f; // Скорость затухания (чем выше, тем быстрее вернется)

    public static SpineProceduralController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetBoneMod(Transform bone, BoneModification mod)
    {
        if (bone == null) return;

        if (!_activeModifications.ContainsKey(bone))
        {
            _activeModifications[bone] = mod;
        }

    }

    public void RemoveBoneMod(Transform bone)
    {
        if (bone == null)
        {
            Debug.LogWarning("SpineProceduralController>RemoveBoneMod не найдена кость ");
            return;
        }


        if (bone != null && _activeModifications.ContainsKey(bone))
        {
            if (!_bonesToRemove.Contains(bone))
            {
                _bonesToRemove.Add(bone);
            }
        }
    }

    void LateUpdate()
    {
        HandleFadeOut();

        if (_activeModifications.Count == 0) return;

        foreach (var entry in _activeModifications)
        {
            Transform bone = entry.Key;
            BoneModification mod = entry.Value;

            if (bone != null)
            {
                // 1. Применяем смещение (Position) с учетом веса
                if (mod.PositionOffset != Vector3.zero)
                {
                    bone.localPosition += mod.PositionOffset * mod.Weight;
                }

                // 2. Применяем поворот (Rotation) с учетом веса
                if (mod.RotationOffset != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.Euler(mod.RotationOffset);
        
                    Quaternion weightedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, mod.Weight);
                    Debug.Log("SpineProceduralController: -> Use Rotate ");
                    bone.localRotation *= weightedRotation;
                }
            }
        }
    }


    private void HandleFadeOut()
    {
        if(_bonesToRemove.Count == 0) return;

        for (int i = _bonesToRemove.Count - 1; i >= 0; i--)
        {
            Transform bone = _bonesToRemove[i];

            if (_activeModifications.TryGetValue(bone, out BoneModification mod))
            {

                Debug.Log("SpineProceduralController: -> stop rotate Weight 1 " + mod.Weight);
                mod.Weight = Mathf.MoveTowards(mod.Weight, 0f, Time.deltaTime * fadeSpeed);
                Debug.Log("SpineProceduralController: -> stop rotate Weight 2 " + mod.Weight);

                if (mod.Weight <= 0)
                {
                    _activeModifications.Remove(bone);
                    _bonesToRemove.RemoveAt(i);
                    Debug.Log("SpineProceduralController: -> stop rotate УДАЛЕНИЕ " + "size _activeModifications " + _activeModifications.Count);
                }
            }
            else
            {
                _bonesToRemove.RemoveAt(i);
            }
        }
        Debug.Log("SpineProceduralController: -> stop rotate " + "size _activeModifications " + _activeModifications.Count);
    }
}
