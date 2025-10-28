using System.Collections;
using UnityEngine;


public class FlyObjectToTarget : MonoBehaviour
{
    private Entity _target;
    private GameObject _source;
    private float _speed;
    private bool _isMoving = false;
    private bool _isRotate = false;
    private float _timeToReachTarget;

    public void Update()
    {
        if (_isMoving)
        {
            _isMoving = false;
            _isRotate = false;
            //MoveToTarget(_target);
            StartCoroutine(MoveToTarget(_target));
        }

       // if (_isRotate)
        //{
        //    Rotate(_target, Time.deltaTime);
       // }
    }

    IEnumerator MoveToTarget(Entity targetPosition)
    {
        Vector3 startingPosition = transform.position;
      
        float elapsedTime = 0f;

        while (elapsedTime < _timeToReachTarget)
        {
            if (_target.IsDead()) break;
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _timeToReachTarget);
            Vector3 test = GetCollision(startingPosition, targetPosition.transform);
            transform.position = Vector3.Lerp(startingPosition, test, t);

            yield return null; 
        }

       
        transform.position = GetCollision(startingPosition, targetPosition.transform);

        Debug.Log("Fly the end " + EffectSkillsmanager.Instance.GetWorldTime() + " ReachTargetTime " + _timeToReachTarget);
    }
   
    public Transform target; // —сылка на NPC, к которому будет двигатьс€ сфера

    private void Rotate(Entity monster, float deltaTime)
    {
        Vector3 direction = (monster.transform.position - transform.position).normalized;
        //direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        if (direction.magnitude > 0)
        {
            //float angle = Quaternion.Angle(transform.rotation, targetRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8.8f * deltaTime);
            //_turnsAround = false;
        }
        else
        {
            //_turnsAround = false;
        }
    }

    public void StartFly(Entity targetEntity, float speed , float timeToReachTarget)
    {
            _target = targetEntity;
            _speed = speed;
            _isMoving = true;
            _timeToReachTarget = timeToReachTarget;
            //_startTine = 0;
           
    }

    public void StartRotate(Entity target)
    {
        _target = target;
    }

    private Vector3 GetCollision(Vector3 attacker , Transform target)
    {
        var heading = attacker - target.position;
        float angle = Vector3.Angle(heading, target.forward);
        float particleHeight = target.GetComponent<Entity>().Appearance.CollisionHeight * 1.25f;
        Vector3 direction = Quaternion.Euler(0, angle, 0) * target.forward;
        return target.position + direction * 0.15f + Vector3.up * particleHeight;
    }
}
