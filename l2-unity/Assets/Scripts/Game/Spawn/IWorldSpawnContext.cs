using UnityEngine;

public interface IWorldSpawnContext
{
    float GetGroundHeight(Vector3 pos);
    Transform UsersContainer { get; }
    Transform NpcsContainer { get; }
    Transform MonstersContainer { get; }
    Transform ItemsContainer { get; }
    IAnimationManager Animations { get; }
    bool ContainsNpc(int id);
    void RegisterPlayer(PlayerEntity player);
    void RegisterUser(Entity user);
    void RegisterNpc(Entity npc);
    void RegisterItem(ItemEntity item);
}
