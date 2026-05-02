using UnityEngine;

public class ModelMovePawn
{

    private Entity _tarEntity;
    private float _dictance;
    private Entity _objPos;
    public ModelMovePawn(Entity objPos , Entity tarEntity, float dictance)
   {
        _tarEntity = tarEntity;
        _dictance = dictance;
        _objPos = objPos;
    }

    public float GetRange()
    {
        float range = _objPos.GetWeaponRage();
        float collsionradius = _objPos.GetCollissionRadius();

        return range + collsionradius;
    }

   public Entity TarEntity()
   {
        return _tarEntity;
   }

    public Vector3 SourObj()
    {
        return _objPos.transform.position;
    }

    public float Distance()
    {
        return _dictance;
    }
}
