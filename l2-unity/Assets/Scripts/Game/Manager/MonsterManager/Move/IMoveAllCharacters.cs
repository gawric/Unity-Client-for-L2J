using UnityEngine;

public interface IMoveAllCharacters
{
    public void AddMoveData(int id, MovementData data);
    public void AddRotate(int id, RotateData data);
    public bool IsMoving(int objId);
    public void CancelMove(int objId);
}
