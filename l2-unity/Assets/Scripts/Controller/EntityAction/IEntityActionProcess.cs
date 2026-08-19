public interface IEntityActionProcess
{
    void Enter(Entity entity, object payload);
    void Tick(Entity entity);
}
