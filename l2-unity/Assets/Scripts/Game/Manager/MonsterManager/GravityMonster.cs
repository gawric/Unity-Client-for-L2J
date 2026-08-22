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
        if (!_isSync)
        {
            return;
        }

        if (_characterController == null ||
            !_characterController.enabled ||
            !_characterController.gameObject.activeInHierarchy ||
            _characterController.radius <= 0.001f ||
            _characterController.height <= 0.001f)
        {
            return;
        }

        if (_entity != null && !_entity.EntityLoaded)
        {
            return;
        }

        if (_entity != null && !_entity.IsDead())
        {
            Vector3 adjustedDirection = _direction * _speed * _moveSpeedMultiplier + Vector3.down * _gravity;
            _characterController.Move(adjustedDirection * Time.deltaTime);
        }
    }

    public void Sync()
    {
        _isSync = true;
    }
}
