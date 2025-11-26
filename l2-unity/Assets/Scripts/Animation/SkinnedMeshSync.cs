using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMeshSync : MonoBehaviour {
    [SerializeField] private SkinnedMeshRenderer _rootSkinnedRenderer;
    [SerializeField] private SkinnedMeshRenderer[] _destSkinnedRenderer;

    private Queue<GameObject> _waitingObjects = new();
    private List<Renderer> _temporarilyHiddenRenderers = new List<Renderer>();
    private bool _isProcessing = false;
    void Start() {
        SyncMesh();
    }




    public void SyncMesh() {
        _destSkinnedRenderer = new SkinnedMeshRenderer[8];
        byte childIndex = 0;


        for (byte i = 0; i < transform.parent.childCount; i++) {
            Transform child = transform.parent.GetChild(i);
            if (child != transform) {
                _destSkinnedRenderer[childIndex++] = child.GetComponentInChildren<SkinnedMeshRenderer>();
            } else {
                _rootSkinnedRenderer = transform.GetComponentInChildren<SkinnedMeshRenderer>();
            }
        }

        foreach (var renderer in _destSkinnedRenderer) {
            if (renderer != null) {
                renderer.bones = _rootSkinnedRenderer.bones;
            }
            //renderer.rootBone = _rootSkinnedRenderer.rootBone;
        }
    }


    public void AddObjectToQueue(GameObject newObject)
    {
        
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot add object: GameObject is inactive");
            return;
        }

        HideRenderers(newObject);
        _waitingObjects.Enqueue(newObject);

        // Если процесс не идет, начинаем его
        if (!_isProcessing)
        {
            StartCoroutine(ProcessWaitingObjects());
        }
    }


    private IEnumerator ProcessWaitingObjects()
    {
        _isProcessing = true;

        while (_waitingObjects.Count > 0)
        {
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("Cannot process objects: GameObject is inactive");
                break;
            }

            var currentObject = _waitingObjects.Dequeue();
            yield return StartCoroutine(AddNewObjectWithWait(currentObject));

            // Небольшая задержка между добавлениями
            yield return new WaitForSeconds(0.1f);
        }

        _isProcessing = false;
    }
    private IEnumerator AddNewObjectWithWait(GameObject newObject)
    {
        int currentCount = transform.parent.childCount;

        if (currentCount < 10)
        {
            newObject.transform.SetParent(transform.parent, false);

            bool success = false;
            yield return StartCoroutine(WaitForChildCountChangeCoroutine(8, result => success = result));

            if (success)
            {

                SyncMesh();
                ShowRenderers(newObject);
            }
            else
            {
                Debug.LogWarning("Не удалось дождаться изменения количества дочерних объектов");
                ShowRenderers(newObject);
            }
        }
        else
        {
            Debug.LogWarning("Достигнут лимит дочерних объектов (8). Невозможно добавить новый объект.");
            yield return false;
        }
    }

    private IEnumerator WaitForChildCountChangeCoroutine(int targetCount, System.Action<bool> callback)
    {
        if (!gameObject.activeInHierarchy)
        {
            callback?.Invoke(false);
            yield break;
        }

        float startTime = Time.time;
        float timeout = 0.3f;
        float lastLogTime = startTime;

        while (transform.parent.childCount > targetCount && Time.time - startTime < timeout)
        {
            if (!gameObject.activeInHierarchy)
            {
                callback?.Invoke(false);
                yield break;
            }

            yield return new WaitForEndOfFrame();
        }

        callback?.Invoke(transform.parent.childCount == targetCount);
    }


    private void HideRenderers(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
            _temporarilyHiddenRenderers.Add(renderer);
        }
    }

    private void ShowRenderers(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (_temporarilyHiddenRenderers.Contains(renderer))
            {
                renderer.enabled = true;
            }
        }
    }

    public void PrintChildrenNames()
    {
        if (transform.parent.childCount == 0)
        {
            Debug.Log("No children found");
            return;
        }

        Debug.Log($"Found {transform.parent.childCount} children:");
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform child = transform.parent.GetChild(i);
            Debug.Log($"[{i}] {child.name}");
        }
    }

}
