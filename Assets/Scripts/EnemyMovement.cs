// using Pathfinding;
using UnityEngine;
using System.Collections;
using Automotion;
using System.Collections.Generic;
// for stats
using TarodevController;
using System;
using UnityEngine.Tilemaps;

namespace Automotion {
    [ RequireComponent ( typeof ( Rigidbody2D ) , typeof ( Collider2D ) ) ]
    public class EnemyMovement : MonoBehaviour
    {
        public Rigidbody2D _rb;
        // public Seeker seeker;
        AStar seeker;
        Path currentPath;
        bool followPath;
        int nextNode = 0;
        CapsuleCollider2D _col;
        bool _grounded;
        bool _reset = false;
        private Vector2 _frameVelocity;
        [Header("Pathfinding")]
        private int countFramesStuck = 0;
        private Vector3 lastPosition;
        private float initialValidationDistance;

        [Header("Pathfinding")]
        [Tooltip("How often should the path be recalculated"), Range(0.1f, 10.0f)]
        public float calculationInterval = 2f; // How often should the path be recalculated
        private float initialPathFindingCalculationInterval;
        [Tooltip("The object that the AI is seeking to reach. This object should be moving, or the AI will never reach it.")]
        public Transform target;
        [Tooltip("The tilemap that the AI will use to compute floor position and obstacles. This tilemap should be the same as the one used by the player.")]
        public Tilemap obstacleTilemap;
        private LayerMask obstacleLayer;
        [Header("Movement Controls")]
        [SerializeField] private ScriptableStats _stats;
        // This variable is used to simulate the commands from the player
        public FrameInput frameInput;

        [Tooltip("How high the AI can jump (related to _stats.JumpPower)"), Range(0,5)]
        public int maxJumpHeight = 2; // How high can the AI jump. This height is somehow related to _stats.JumpPower. Could be automatically calculated.
        [Tooltip("How far to check for a node before deciding there's nothing there"), Range(0.1f, 3.0f)]
        public float validationDistance = 0.75f; // How far to check for a node before deciding there's nothing there
        [Header("Gap Calculation")]
        [Tooltip("MAX_JUMP_LENGTH"), Range(5, 50)]
        private int LOOK_AHEAD = 40; // How many nodes to use when checking for gaps. Larger values may mean gaps are recognised too early.
        // TODO: Stairs detection. This feature is not implemented
        // [Header("Reset")]
        // [Tooltip("If true, the AI will reset to its original position and start over")]
        // TODO: See how to implement this reset properly
        // public bool resetRequested = false;

        void Awake() {
            obstacleLayer = obstacleTilemap.gameObject.layer;
        }
        void Start()
        {
            Reset();
        }

        public void Reset() {
            // resetRequested = false;
            _reset = true;
            Debug.Log("Resetting");
            StopCoroutine(CalculatePath());
            StopCoroutine(FollowPath());
            followPath = false;
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            // seeker = GetComponent<Seeker>();
            seeker = GetComponent<AStar>();
            obstacleLayer = obstacleTilemap.gameObject.layer;
            nextNode = 0;
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0;
            new WaitForSeconds(1f);
            Debug.Log("Restarting");
            _reset = false;
            StartCoroutine(CalculatePath());
            StartCoroutine(FollowPath());
        }

        void OnValidate() {
            initialValidationDistance = validationDistance;
            initialPathFindingCalculationInterval = calculationInterval;
        }

         private void FixedUpdate()
        {
            // if (resetRequested) Reset();
            _frameVelocity = _rb.velocity;
            HandleMovement();
            HandleStuck();
            HandleGravity();
            ApplyMovement();
        }

        void HandleStuck() {
            // If the AI is stuck, against a wall or something, it will reset
            if (lastPosition == transform.position)
            {
                countFramesStuck++;
                if (countFramesStuck > 100 && countFramesStuck < 200)
                {
                    _frameVelocity = Vector2.zero;
                    frameInput = new FrameInput();
                    followPath = false;
                    validationDistance *= .9f;
                    calculationInterval = 0.1f;
                }
                if (countFramesStuck > 200) {
                    _frameVelocity.x *= -5f;
                }
            }
            else
            {
                validationDistance = initialValidationDistance;
                calculationInterval = initialPathFindingCalculationInterval;
                countFramesStuck = 0;
            }
            lastPosition = transform.position;
        }
        IEnumerator CalculatePath()
        {
            followPath = false; // prevents the object from trying to follow the path before it's ready
            // seeker.StartPath(transform.position, target.position, OnPathComplete);
            currentPath = seeker.FindPath(Vector2Int.FloorToInt(transform.position), Vector2Int.FloorToInt(target.position), obstacleTilemap, maxJumpHeight);
            if (currentPath.path == null)
            {
                Debug.Log("No path found");
            }
            else
            {
                Debug.Log("Path calculated with " + currentPath.path?.Count + " nodes");
                followPath = true;
                nextNode = 0;
            }
            yield return new WaitForSeconds(calculationInterval);
            if (!_reset) StartCoroutine(CalculatePath());
        }
        void OnPathComplete(Path path)
        {
            currentPath = path;
            nextNode = 0;
            followPath = true;
            Debug.Log("Path calculated with " + currentPath.path.Count + " nodes");
        }

        void HandleMovement() {
            if (frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        void HandleGravity() {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else {
                if (_grounded && frameInput.JumpDown)
                {
                    _frameVelocity.y = _stats.JumpPower;
                }
                var inAirGravity = _stats.FallAcceleration;
                bool _endedJumpEarly = false;
                if (!_grounded && !frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
            _rb.velocity = _frameVelocity;
            string debugMessage = "[HandleMovement] Velocity: " + _rb.velocity + " grounded: " + _grounded + " jump: " + frameInput.JumpDown + " held: " + frameInput.JumpHeld + " follow: " + followPath + " move: " + frameInput.Move + " at " + transform.position + " to " + target.position;
            if (currentPath.path != null && nextNode < currentPath.path.Count)
            {
                debugMessage += " with " + currentPath.path.Count + " nodes. Next node: " + nextNode + " at " + currentPath.path[nextNode].position;
            }
            Debug.Log(debugMessage);
        }

        void ApplyMovement() {
            _rb.velocity = _frameVelocity;
        }

        IEnumerator FollowPath()
        {
            FrameInput oldFrameInput = frameInput;
            frameInput = new FrameInput();
            if (followPath)
            {
                Vector2 jump = Jump();
                if (jump != Vector2.zero && CanJump())
                {
                    frameInput.JumpDown = true;
                    frameInput.JumpHeld = true;
                    frameInput.Move = jump;
                    Debug.Log("[FollowPath] Jumping: " + jump + " at " + transform.position + " to " + target.position + " with " + currentPath.path.Count + " nodes. Next node: " + nextNode + " at " + currentPath.path[nextNode].position);
                }
                else if (nextNode >= currentPath.path.Count)
                {
                    followPath = false;
                    Debug.Log("[FollowPath] Reached the end of the path");
                }
                else {
                    Vector3 targetPosition = new Vector3(currentPath.path[nextNode].position.x, currentPath.path[nextNode].position.y, transform.position.z);
                    Vector3 moveDirection = (Vector3)targetPosition - transform.position;
                    Debug.Log("[FollowPath] Distance: " + Vector2.Distance(transform.position, targetPosition) + " at speed " + moveDirection + " to " + targetPosition + " from " + transform.position + ". x distance: " + Math.Abs(transform.position.x - targetPosition.x) + " JumpHeld " + oldFrameInput.JumpHeld + " followPath " + followPath);

                    bool acceptableWalkingAccuracy = (Vector2.Distance(transform.position, targetPosition) < validationDistance && !oldFrameInput.JumpHeld);
                    bool acceptableJumpingAccuracy = (Math.Abs(transform.position.x - targetPosition.x) < validationDistance  && currentPath.path[nextNode].jumpScore > 0 && oldFrameInput.JumpHeld);
                    // I want to be extra careful when I land on a node after jumping
                    bool acceptableLandingAccuracy = (Vector2.Distance(transform.position, targetPosition) < validationDistance/2 && currentPath.path[nextNode].jumpScore == 0 && oldFrameInput.JumpHeld);
                    Debug.Log("[FollowPath] Acceptable walking accuracy: " + acceptableWalkingAccuracy + " acceptable jumping accuracy: " + acceptableJumpingAccuracy + " acceptable landing accuracy: " + acceptableLandingAccuracy);
                    while (followPath && (acceptableWalkingAccuracy || acceptableJumpingAccuracy || acceptableLandingAccuracy))
                    {
                        if (nextNode < currentPath.path.Count) nextNode++;
                        if (nextNode >= currentPath.path.Count) {
                            followPath = false;
                            Debug.Log("[FollowPath] Reached the end of the path");
                            continue;
                        }
                        targetPosition = new Vector3(currentPath.path[nextNode].position.x, currentPath.path[nextNode].position.y, transform.position.z);
                        moveDirection += ((Vector3)targetPosition - transform.position)/2;
                        Debug.Log("[FollowPath] Next node: " + nextNode + " at " + targetPosition);
                        acceptableWalkingAccuracy = (Vector2.Distance(transform.position, targetPosition) < validationDistance && !oldFrameInput.JumpHeld);
                        acceptableJumpingAccuracy = (Math.Abs(transform.position.x - targetPosition.x) < validationDistance  && currentPath.path[nextNode].jumpScore > 0 && oldFrameInput.JumpHeld);
                        acceptableLandingAccuracy = (Vector2.Distance(transform.position, targetPosition) < validationDistance/2 && currentPath.path[nextNode].jumpScore == 0 && oldFrameInput.JumpHeld);
                        Debug.Log("[FollowPath] Acceptable walking accuracy: " + acceptableWalkingAccuracy + " acceptable jumping accuracy: " + acceptableJumpingAccuracy + " acceptable landing accuracy: " + acceptableLandingAccuracy);
                    }
                    frameInput.Move = moveDirection;
                    if (!_grounded)
                    {
                        frameInput.JumpHeld = true;
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
            if (!_reset) StartCoroutine(FollowPath());
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log("Collided with " + collision.gameObject.name + " at " + collision.contacts[0].point);
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                if (collision.contacts[i].point.y < transform.position.y - 0.1f && 
                    Math.Abs(collision.contacts[i].point.x - transform.position.x) < 0.1f)
                {
                    _grounded = true;
                }
            }
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            Debug.Log("Stay Collided with " + collision.gameObject.name + " at " + collision.contacts[0].point);
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                if (collision.contacts[i].point.y < transform.position.y - 0.1f && 
                    Math.Abs(collision.contacts[i].point.x - transform.position.x) < 0.1f)
                {
                    _grounded = true;
                }
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            Debug.Log("Exited collision with " + collision.gameObject.name);
            _grounded = false;
        }

        bool CanJump()
        {
            bool isCeilingHit = (checkCeiling().collider != null) && false;
            return _grounded && !isCeilingHit;
        }

        Vector2 Jump()
        {
            Vector2? jumpStart = null;
            Vector2? jumpEnd = null;
            if (_grounded && jumpStart == null && 
                nextNode < currentPath.path.Count &&
                currentPath.path[nextNode].jumpScore > 0)
            {
                jumpStart = transform.position;
            }
            for (int i = 1; i < LOOK_AHEAD && jumpEnd == null && (jumpStart != null); i++)
            {
                if (nextNode + i < currentPath.path.Count && 
                    currentPath.path[nextNode + i].jumpScore == 0 && jumpStart != null)
                {
                    jumpEnd = currentPath.path[nextNode + i].position;
                }
            }
            if (jumpEnd == null)
            {
                return Vector2.zero;
            }
            Vector2 fixedVy = ComputeJumpFixedVY((Vector2)jumpStart, (Vector2)jumpEnd, _stats.FallAcceleration);
            Vector2 fixedVx = ComputeJumpFixedVX((Vector2)jumpStart, (Vector2)jumpEnd, _stats.FallAcceleration);
            // if (Math.Abs(fixedVy.x) < _stats.MaxSpeed)
            // {
            //     return fixedVx;
            // }
            // else
            // {
                return fixedVy;
            // }
        }

        Vector2 ComputeJumpFixedVX(Vector2 from, Vector2 to, float gravity)
        {
            float distance = to.x - from.x;
            float vx = _stats.MaxSpeed * Mathf.Sign(distance);
            float time = Math.Abs(distance / vx);
            float height = to.y - from.y;
            float vy = height / time + gravity * time;
            Debug.Log("[ComputeJump] Jumping from " + from + " to " + to + " with distance " + distance + " and time " + time + " and velocity (" + vx + ", " + vy+")");
            return new Vector2(vx, vy);
        }

        Vector2 ComputeJumpFixedVY(Vector2 from, Vector2 to, float gravity)
        {
            float vy = _stats.JumpPower;
            float distance = to.x - from.x;
            float height = to.y - from.y;
            float time = (float)(2*vy + Math.Sqrt(vy*vy + 2 * height * gravity)) / gravity;
            float vx = distance / time;
            Debug.Log("[ComputeJump] Jumping from " + from + " to " + to + " with distance " + distance + " and time " + time + " and velocity (" + vx + ", " + vy+")");
            return new Vector2(vx, vy);
        }

        private RaycastHit2D checkFloor()
        {
            return checkHit(Vector2.down);
        }

        private RaycastHit2D checkCeiling() {
            return checkHit(Vector2.up);
        }

        private RaycastHit2D checkHit(Vector2 direction) {
            return checkHit(_rb.transform.position, direction);
        }

        private RaycastHit2D checkHit(Vector2 from, Vector2 direction) {
            float size = _col.size.y * transform.localScale.y * 0.5f + 0.01f;
            if (direction.x != 0)
            {
                size = _col.size.x * transform.localScale.x * 0.5f + 0.51f;
            }
            // Debug.DrawRay(from, direction * size, Color.red);
            Debug.Log("Checking for " + direction + " from " + from + " with size " + size);
            return Physics2D.CapsuleCast(from, _col.size, _col.direction, 0, direction, size, obstacleLayer);
        }

        private void OnDrawGizmos()
        {
            if (currentPath.path != null)
            {
                for (int i = 0; i < currentPath.path.Count; i++)
                {
                    Gizmos.color = (i < nextNode?Color.red:Color.green);
                    Vector3 position = new Vector3(currentPath.path[i].position.x, currentPath.path[i].position.y, transform.position.z);
                    if (currentPath.path[i].jumpScore > 0)
                    {
                        Gizmos.DrawCube(position, new Vector3(0.1f, 0.1f, 0.1f));
                    }
                    else
                    {
                        Gizmos.DrawSphere(position, 0.1f);
                    }
                }
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)frameInput.Move);
            if (frameInput.JumpDown)
            {
                Gizmos.color = Color.red;
            }
            else if (frameInput.JumpHeld)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.grey;
            }
            Gizmos.DrawSphere(transform.position + new Vector3(0, 0.5f, 0), 0.2f);
            if (_grounded)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.grey;
            }
            Gizmos.DrawSphere(transform.position + new Vector3(0, -0.5f, 0), 0.1f);
        }
    }
}