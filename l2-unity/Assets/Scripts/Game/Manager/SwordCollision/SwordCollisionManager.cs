using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;


public class SwordCollisionService : MonoBehaviour
{
    public static SwordCollisionService Instance { get; private set; }

    public LayerMask _entityMask;
    private List<TrackedSword> activeSwords = new List<TrackedSword>();
    private Dictionary<Transform, Vector3> lastPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, HashSet<int>> hitRegistry = new Dictionary<Transform, HashSet<int>>();
    private TrackedSword[] _warningDetele = new TrackedSword[3] { null, null, null};
    //public event Action<RaycastHit, Transform, Transform> OnHitCollider;

    public event Action<Transform, Transform, Vector3, Vector3> OnHitCollider;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _entityMask = LayerMask.GetMask("EntityClick");
    }


    public void RegisterSword(Transform swordBase, Transform swordTip , Transform target , float extraRange)
    {
        if (swordBase == null || swordTip == null) return;

        if(target == null)
        {
            Debug.LogWarning("SwordCollisionManager: RegisterSword: target == null mob is Dead?");
            return;
        }

        if (activeSwords.Exists(s => s.basePt == swordBase))
        {
            ResetHitRegistry(swordBase);
            return;
        }


        //Debug.Log("SwordCollisionManager: RegisterSword");
        activeSwords.Add(new TrackedSword(swordBase, swordTip , target ,  extraRange));
        lastPositions[swordBase] = swordBase.position;
        lastPositions[swordTip] = swordTip.position;
        ResetHitRegistry(swordBase);
    }


    public void ResetHitRegistry(Transform swordBase)
    {
        if (!hitRegistry.ContainsKey(swordBase)) hitRegistry[swordBase] = new HashSet<int>();
        hitRegistry[swordBase].Clear();
    }

    public void UnregisterSword(Transform swordBase)
    {
        TrackedSword sword = activeSwords.Find(s => s.basePt == swordBase);
        //Debug.Log("SwordCollisionManager: Удаление RegisterSword");
        if (sword != null)
        {
            activeSwords.Remove(sword);
            lastPositions.Remove(sword.basePt);
            lastPositions.Remove(sword.tipPt);
            hitRegistry.Remove(swordBase);
        }
    }

    void LateUpdate()
    {
        if (activeSwords.Count == 0) return;

        for (int i = activeSwords.Count - 1; i >= 0; i--)
        {
            var sword = activeSwords[i];
            if (sword.basePt == null || sword.tipPt == null) continue;

            Vector3 currentBase = sword.basePt.position;
            Vector3 currentTip = sword.tipPt.position;

  
            if (lastPositions.ContainsKey(sword.basePt) && lastPositions.ContainsKey(sword.tipPt))
            {
                Vector3 prevBase = lastPositions[sword.basePt];
                Vector3 prevTip = lastPositions[sword.tipPt];

                // ГЛАВНЫЙ МЕТОД: Проверка "полотна" взмаха
                CheckSwordSwingPanel(prevBase, prevTip, currentBase, currentTip, sword , _warningDetele);
            }

            if (_warningDetele[0] != null)
            {
                UnregisterSword(_warningDetele[0].basePt);
                _warningDetele[0] = null;

                Debug.LogWarning("SwordCollisionManager: Sword was deleted " + PlayerEntity.Instance.RandomName);
            }
      
            lastPositions[sword.basePt] = currentBase;
            lastPositions[sword.tipPt] = currentTip;
        }
    }


    private void CheckSwordSwingPanel(Vector3 prevBase, Vector3 prevTip, Vector3 currBase, Vector3 currTip, TrackedSword sword , TrackedSword[] warningDetele)
    {
        int rayCount = 6;
        float swordThickness = 0.5f; // Радиус сферы (толщина лезвия)
        float forwardReach = 0.4f;   // На сколько ПЕРЕД мечом мы ищем цель (те самые 20-30 см)

        for (int i = 0; i <= rayCount; i++)
        {
            float t = (float)i / rayCount;

            Vector3 start = Vector3.Lerp(prevBase, prevTip, t);
            Vector3 end = Vector3.Lerp(currBase, currTip, t);

            // Направление движения этой точки меча
            Vector3 movementDir = (end - start).normalized;
            // Расстояние, которое прошла точка + запас вперед
            float dist = Vector3.Distance(start, end) + forwardReach;

            if (dist > 0.01f)
            {


 
                if (start == null)
                {
                    Debug.LogError("SwordCollisionService: 'start' reference is null (object destroyed)");
                    return;
                }

        
                if (swordThickness <= 0)
                {
                    Debug.LogError("SwordCollisionService: 'swordThickness' is invalid (value: " + swordThickness + ")");
                    return;
                }

           
                if (movementDir == null || movementDir.sqrMagnitude == 0)
                {
                    Debug.LogError("SwordCollisionService: 'movementDir' is null or zero vector");
                    warningDetele[0] = sword;
                    return;
                }

                if (Physics.SphereCast(start, swordThickness, movementDir, out RaycastHit hit, dist, _entityMask))
                {
                    Debug.Log("ON HIT! 1");
                    if (RegisterHit(sword.basePt, hit.collider.GetInstanceID()))
                    {
                        try
                        {
                            Debug.Log("ON HIT! 2");
                            OnHit(hit, sword);
                            //OnHitCollider?.Invoke(hit, sword.basePt.parent, sword.tipPt);
                            Debug.Log("SwordCollisionService: HIT Monster!");
                            //DebugLineDraw.ShowDrawLineDebugNpc(sword.basePt.GetInstanceID(), start, hit.point, Color.red);
                        }
                        catch (Exception e) { 
                            Debug.LogError(e);
                        }


                    }
                }
                else
                {
                    // Рисуем желтую линию — это то, где мы искали
                   // DebugLineDraw.ShowDrawLineDebugNpc(sword.basePt.GetInstanceID(), start, start + movementDir * dist, Color.yellow);
                }
            }
        }
    }

    // Вспомогательный метод для реестра попаданий (чтобы один взмах не бил 100 раз по одной цели)
    private bool RegisterHit(Transform swordBase, int targetID)
    {
        if (!hitRegistry.ContainsKey(swordBase)) hitRegistry[swordBase] = new HashSet<int>();

        if (hitRegistry[swordBase].Contains(targetID)) return false;

        hitRegistry[swordBase].Add(targetID);
        return true;
    }





    private void OnHit(RaycastHit hit, TrackedSword sword)
    {
        // Check if hit.transform is valid
        Debug.Log("ON HIT! 3");
        if (hit.transform == null)
        {
            Debug.LogError("OnHit: hit.transform is null");
            return;
        }

        // Check if hit.transform.parent is valid
        Debug.Log("ON HIT! 4");
        if (hit.transform.parent == null)
        {
            Debug.LogError("OnHit: hit.transform.parent is null");
            return;
        }

        Debug.Log("ON HIT! 5");
        GameObject gameObject = hit.transform.parent.gameObject;
        Debug.Log("ON HIT! 6");
        Entity entity = gameObject.GetComponent<Entity>();
        Debug.Log("ON HIT! 7");
        // Check if sword is valid
        if (sword == null)
        {
            Debug.Log("ON HIT! 8");
            Debug.LogError("OnHit: sword is null");
            return;
        }

        Debug.Log("ON HIT! 9");
        // Check if sword.target is valid
        if (sword.target == null)
        {
            Debug.Log("ON HIT! 10");
            Debug.LogError("OnHit: sword.target is null");
            return;
        }

        Debug.Log("ON HIT! 11");
        GameObject targetGameObject = sword.target.gameObject;
        Debug.Log("ON HIT! 12");
        Entity targetEntity = targetGameObject.GetComponent<Entity>();

        Debug.Log("ON HIT! 13");
        if (targetEntity == null || entity == null)
        {
            Debug.Log("ON HIT! 14");
            Debug.LogWarning("OnHit: Either targetEntity or entity is null");
            return;
        }

        Debug.Log("ON HIT! 15");
        // Check if sword.basePt is valid
        if (sword.basePt == null)
        {
            Debug.Log("ON HIT! 16");
            Debug.LogError("OnHit: sword.basePt is null");
            return;
        }

        Debug.Log("ON HIT! 17");
        if (sword.basePt.parent == null)
        {
            Debug.LogError("OnHit: sword.basePt.parent is null");
            return;
        }

        Debug.Log("ON HIT! 18");
        if (entity.IdentityInterlude.Id == targetEntity.IdentityInterlude.Id)
        {
            Debug.Log("ON HIT! 19");
            Vector3 startPos = sword.basePt.position;

            // Check if hit.point is valid
            if (hit.point == null)
            {
                Debug.Log("ON HIT! 20");
                Debug.LogError("OnHit: hit.point is null");
                return;
            }

            Debug.Log("ON HIT! 21");
            var hitDirection = VectorUtils.CalcHitDirection(hit.point, startPos);

            // Check if OnHitCollider is valid before invoking
            if (OnHitCollider != null)
            {
                Debug.Log("ON HIT! 22");
                OnHitCollider?.Invoke(sword.basePt.parent, sword.target, hit.point, hitDirection);
            }
            else
            {
                Debug.Log("ON HIT! 23");
                Debug.LogWarning("OnHit: OnHitCollider event is null");
            }
        }
    }



}
