using UnityEngine;

public class GravityMonster : MonoBehaviour
{
    private NetworkAnimationController _animationReceive;
    private Entity _entity;
    private CharacterController _characterController;

    private Vector3 _direction;
    private float _speed;
    private Vector3 _destination;
    private float _gravity = 28f;
    private float _moveSpeedMultiplier = 1f;
    private bool _isSync = false;
    public NetworkAnimationController NetworkAnimationController { get { return _animationReceive; } }
    void Start()
    {
        

        _animationReceive = GetComponent<NetworkAnimationController>();
        _entity = GetComponent<Entity>();
        _characterController = GetComponent<CharacterController>();

        _direction = Vector3.zero;
        _destination = Vector3.zero;
    }


    private void FixedUpdate()
    {
        if (!_isSync) return;

        if (!_entity.IsDead())
        {
            Vector3 ajustedDirection = _direction * _speed * _moveSpeedMultiplier + Vector3.down * _gravity;
            _characterController.Move(ajustedDirection * Time.deltaTime);
            //Debug.Log("Position IsDead SetMoveDirectionToDestination");
        }

    }

    public void Sync()
    {
        _isSync = true;
    }
}
