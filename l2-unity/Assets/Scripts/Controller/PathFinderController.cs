using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PathFinderController : MonoBehaviour
{
    [Header("Path data")]
    [SerializeField] private Node _startNode;
    [SerializeField] private Node _targetNode;
     private Transform _targetDestinationPawn;
     private Vector3 _targetDestinationVector;
    [SerializeField] private float _defaultDestinationThreshold = 0.10f;
    [SerializeField] private float _currentDestinationThreshold = 0.10f;
    [SerializeField] private List<Node> _path;
    [SerializeField] private bool _simplifyPath = true;

    [Header("Ground check")]
    [SerializeField] private ObjectData _collidingWith;
    [SerializeField] private LayerMask _ignoredLayers;
    [SerializeField] private bool _grounded;

    [SerializeField] private bool _debugPathFinder;

    private CharacterController _characterController;

    private static PathFinderController _instance;
    public static PathFinderController Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    //void Start()
    //{
     //   _characterController = GetComponent<CharacterController>();
    //}

    //public void Update()
   // {
        // Update initial node
        //if (_characterController.isGrounded && _collidingWith != null)
        //{
           /// try
            //{
               // _startNode = Geodata.Instance.GetNodeAt(transform.position);
            //}
            //catch (Exception)
            //{
             //   _startNode = null;
           // }
       // }

        // Reset path when user input
        //if (InputManager.Instance.Move)
        //{
         //   ClearPath();
       // }
    //}

    //public void ClearPath()
   // {
      //  _path.Clear();
   // }

   // public Vector3 GetCurrentPosition()
    //{
     //   return transform.position;
    //}

   // public void FixedUpdate()
    //{
        //Vector3 flatTransformPos = VectorUtils.To2D(transform.position);
       // Vector3 flatDestPos = VectorUtils.To2D(_targetDestinationVector);

       //if (_path.Count > 0)
       // {
            // If path has more than one node remaining run to node center
            // Otherwise run to destination
           // if (_path.Count > 1)
           // {
           //     flatDestPos = VectorUtils.To2D(_path[0].center);
           // }

            // Remove node from path when node reached
           // if (Vector3.Distance(flatDestPos, flatTransformPos) < _currentDestinationThreshold)
           // {
             //   _path.RemoveAt(0);
            //}

            // If a node is remaning update the target position
            //if (_path.Count > 0)
            //{

               // PlayerController.Instance.SetDestinationVector(_targetDestinationVector, _currentDestinationThreshold);
            //}
        //}
        //else
        //{
            //var distance = Vector3.Distance(flatDestPos, flatTransformPos);
            //float roundedDistance = Mathf.Floor(distance * 10) / 10;
            //else equals
            //if (roundedDistance == _currentDestinationThreshold)
           // {
            //    PlayerController.Instance.ResetDestination();
           // }
           // else
           // {
               // if (Vector3.Distance(flatDestPos, flatTransformPos) < _currentDestinationThreshold)
              //  {
                 //   PlayerController.Instance.ResetDestination();
                //}
          // }

  
        //}
   // }


   // public void MoveTo(Vector3 destination, float stopAtRange)
    //{
       // _currentDestinationThreshold = stopAtRange;
       // _targetDestinationVector = destination;

        //Node node = null;
        //try
       // {
         //   node = Geodata.Instance.GetNodeAt(_targetDestinationVector);
        //}
        //catch (Exception) { }

        //if (node != null && _startNode != null)
        //{
           // _targetNode = node;
           // PathFinderFactory.Instance.RequestPathfind(_startNode, _targetNode, (callback) =>
            //{
                //if (callback == null || callback.Count == 0)
                //{

                   // PlayerController.Instance.SetDestinationVector(_targetDestinationVector, _currentDestinationThreshold);
                //}
                //else
                //{

                    //if (_simplifyPath)
                    //{
                        //_path = PathFinder.SmoothPath(callback);
                     //   Debug.Log(" �� �������� ����� PathFinderController �� �������� ");
                    //}
                    //else
                   // {
                   //     _path = callback;
                   // }
              //  }
           // });

       // }
       // else
        //{
            //Debug.Log("����������� 1");
           // _targetNode = null;

          //  PlayerController.Instance.SetDestinationVector(_targetDestinationVector, _currentDestinationThreshold);
       // }
   // }

    //void OnControllerColliderHit(ControllerColliderHit hit)
    //{
       // if (_ignoredLayers != (_ignoredLayers | (1 << hit.gameObject.layer)))
       /// {
         //   _collidingWith = new ObjectData(hit.gameObject);
       // }
        //else
       // {
          //  _collidingWith = null;
       // }
   // }

   // void OnDrawGizmos()
   // {
       // if (!_debugPathFinder || !Application.isPlaying)
       //     return;

       // float nodeSize = Geodata.Instance.NodeSize;
       // Vector3 cubeSize = new Vector3(nodeSize - nodeSize / 10f, 0.1f, nodeSize - nodeSize / 10f);

       // Gizmos.color = Color.yellow;

        //if (_targetNode != null)
       // {
         //   Gizmos.DrawCube(_targetNode.center + Vector3.up * 0.1f, cubeSize);
       // }

        //Gizmos.color = Color.green;
       // if (_startNode != null)
       // {
        //    Gizmos.DrawCube(_startNode.center + Vector3.up * 0.1f, cubeSize);
        //}

        //Gizmos.color = Color.white;

       // if (_path != null && _path.Count > 0)
      //  {
            //foreach (var node in _path)
           // {
            //    Gizmos.DrawCube(node.center + Vector3.up * 0.1f, cubeSize);
          //  }
      // }
    //}
}
