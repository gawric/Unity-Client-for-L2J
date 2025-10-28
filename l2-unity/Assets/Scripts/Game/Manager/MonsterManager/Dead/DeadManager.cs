using System.Collections.Generic;
using UnityEngine;

public class DeadManager : MonoBehaviour, IDead
{
    private static IDead _instance;
    public static IDead Instance { get { return _instance; } }

    private Dictionary<int, DeadData> _dict;
    public float speed = 0.000001f; // Скорость движения вверх
    List<int> _remove = new List<int>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _dict = new Dictionary<int, DeadData>();
            _remove = new List<int>();
        }
        else
        {
            Destroy(this);
        }
    }
    
    void Update()
    {
        if (_dict.Count == 0) return;

        

        foreach (KeyValuePair<int, DeadData> kvp in _dict)
        {
            DeadData data = kvp.Value;

           

            if (data.IsAntiGravity())
            {
                float newY3 = transform.position.y;
                float newY4 = newY3 * 2;
                float zeroPosition = newY3 - newY4;
                data.SetCurrentPos(zeroPosition - zeroPosition * 2);
                data.SetZeroPos(data.GetCurrentPos());
                data.SetCurrentPos(data.GetCurrentPos() + 0.5f);
                //float lerp = Mathf.Lerp(transform.position.y, 0.5f, speed * Time.deltaTime);
               // _monsterRenderer = gameObject.GetComponentsInChildren<Renderer>();
                data.SetRefresh(true);
                data.SetAntiGravity(false);
    
                NameplatesManager.Instance.Remove(data.GetIdEntity());
            }
            else
            {
                if (data.IsRefresh())
                {
                    if (transform.position.y <= 3)
                    {
                        var originalColor = GetOriginalColor(data.GetRender());
                        if (originalColor.a > 0.1)
                        {
                            float lerp = Mathf.Lerp(transform.position.y, data.GetCurrentPos(), speed * Time.deltaTime);
                            float alpha = Mathf.Lerp(originalColor.a, 0f, 1f * Time.deltaTime);
                            transform.position = new Vector3(transform.position.x, lerp, transform.position.z);
                            SetOpacity(data.GetRender(), alpha);
                            //_remove.Add(kvp.Key);
                        }
                        else
                        {
                            World.Instance.RemoveObject(data.GetIdEntity());
                            data.SetRefresh(false);
                            _remove.Add(kvp.Key);
                        }

                    }

                }

            }
        }
        //Debug.Log("DeadManager size 1 " + _dict.Count);
        Remove(_remove);
        //Debug.Log("DeadManager size 2 " + _dict.Count);
    }


    public void AddDeadAndRemove(int id , DeadData data)
    {
        if (!_dict.ContainsKey(id))
        {
            _dict.Add(id, data);
        }
    }
    private void Remove(List<int> remove)
    {
        foreach(int id in remove)
        {
            if (_dict.ContainsKey(id))
            {
                _dict.Remove(id);
            }
        }

        remove.Clear();
    }

    public Color GetOriginalColor(Renderer[] monsterRenderer)
    {
        if (monsterRenderer[0] != null)
        {
            Material material = monsterRenderer[0].material;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1); // 1 для Transparent, 0 для Opaque
            }

            if (material != null)
            {
                return material.color;
            }
        }
        return new Color();
    }

    private void SetOpacity(Renderer[] monsterRenderer, float opacity)
    {
        if (monsterRenderer[0] != null)
        {
            Material material = monsterRenderer[0].material;
            if (material != null)
            {
                // need change setting material manual _Surface = 1, RenderType = Transperenty
                material.SetFloat("_Surface", 1); // 1 = Transparent, 0 = Opaque
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Изменяем цвет и устанавливаем прозрачность
                Color color = material.color;
                color.a = opacity;
                material.color = color;
            }
        }

    }
}
