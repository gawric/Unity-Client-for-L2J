using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.EventSystems.EventTrigger;

public class NpcCursorManager : MonoBehaviour
{

    private Texture2D _defaultCursor;
    private Texture2D _hoverCursorTalk;
    private Texture2D _hoverCursorAtk;
    private Collider[] colliders;
    private LayerMask _entityMask = 8192;
    private ObjectData _hoverObjectData;
    private ObjectData lastHoveredObject;
    private ObjectData _targetObjectData;
    private int _currentCursor;
    private int defaultCursorId = 0;
    private int atkCursorId = 1;
    private int talkCursorId = 2;
    void Start()
    {
        _defaultCursor = IconManager.Instance.LoadCursorByName("Default");
        _hoverCursorAtk = IconManager.Instance.LoadCursorByName("Attack");
        _hoverCursorTalk = IconManager.Instance.LoadCursorByName("Talk");
        _currentCursor = -1;

    }

    void Update()
    {
        // ������ ��� �� ������� ���� � 3D-������������
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, 1000f, ~23158))
        {
        
            //GameObject hoveredObject = hit.collider.gameObject;
            int hitLayer = hit.collider.gameObject.layer;
            if (_entityMask == (_entityMask | (1 << hitLayer)))
            {
                _hoverObjectData = new ObjectData(hit.transform.parent.gameObject);
            }
            else
            {
                _hoverObjectData = new ObjectData(hit.collider.gameObject);
            }

            _targetObjectData = _hoverObjectData;
            if(hitLayer == 13)
            {
                int targetID = PlayerEntity.Instance.TargetId;
                if(_targetObjectData != null)
                {
                    if(_targetObjectData.Entity != null)
                    {
                        if (_targetObjectData.Entity.IdentityInterlude.Id == targetID)
                        {
                            OnMouseEnterTest(_targetObjectData);
                        }
                        else
                        {
                            OnMouseExitTest(_targetObjectData);
                        }
                    }
                    else
                    {
                        OnMouseExitTest(_targetObjectData);
                    }
                    
                }
           
            }
            else
            {
                OnMouseExitTest(lastHoveredObject);
            }
           
        }

    }

   private  void OnMouseEnterTest(ObjectData obj)
    {
        if(obj.Entity != null)
        {
            Entity entity  = obj.Entity;
            if (entity.GetType() == typeof(MonsterEntity))
            {
                if(_currentCursor == -1 | _currentCursor != 1)
                {
                    _currentCursor = 1;
                    Cursor.SetCursor(_hoverCursorAtk, Vector2.zero, CursorMode.Auto);
                    //Debug.Log("Cursor Moster Set ");
                }
                
            }
            else if(entity.GetType() == typeof(NpcEntity))
            {
                if (_currentCursor == -1 | _currentCursor != 2)
                {
                    _currentCursor = 2;
                    Cursor.SetCursor(_hoverCursorTalk, Vector2.zero, CursorMode.Auto);
                }
                    
            }
        }
  
    }

    private void OnMouseEnter()
    {
       

    }

    private void OnMouseExit()
    {
        
    }

    private void OnMouseExitTest(ObjectData obj)
    {
        if (_currentCursor == -1 | _currentCursor != 0)
        {
            _currentCursor = 0;
            Cursor.SetCursor(_defaultCursor, Vector2.zero, CursorMode.Auto);
        }
           
        //Debug.Log("������ �������� � ������������.");
    }
}

