using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TarodevController;

namespace Automotion
{
    public class Automotion : MonoBehaviour
    {

        [SerializeField]
        private GameObject player;
        [SerializeField]
        private Tilemap tilemap;
        public float speed = 2f;
        private Platforms platforms = new Platforms(true);
        public FrameInput FrameInput;
        private PossibleActions previousAction;
        private PlatformCell startJumpCell;


        void Awake()
        {
            player = GameObject.Find("Player");
            tilemap = GameObject.Find("Tilemap").GetComponent<Tilemap>();
            // ComputePlatforms();
            // ComputeReachableNeighbours();
        }

        // Start is called before the first frame update
        void Start()
        {
			StartCoroutine(Execute());
        }


        // Update is called once per frame
        IEnumerator Execute()
        {
			Move(Vector2Int.right);
			yield return new WaitForSeconds(0.1f);
			StartCoroutine(Execute());
        }


        // this method is computing all the platforms
        // and stores them in a list
        // A platform is composed of coordinates and a type
        void ComputePlatforms()
        {
            // Debug.Log("Computing platforms from "+tilemap.cellBounds.xMin+" to "+tilemap.cellBounds.xMax+" and from "+tilemap.cellBounds.yMin+" to "+tilemap.cellBounds.yMax);
            for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
            {
                for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    // if the cell is empty and the cell below is not
                    if (IsGround(cell))
                    {
                        PlatformType type = PlatformType.Ground;
                        if (!IsGround(cell + Vector2Int.left) && IsGround(cell + Vector2Int.right))
                        {
                            type = PlatformType.LeftEdge;
                        }
                        else if (!IsGround(cell + Vector2Int.right) && IsGround(cell + Vector2Int.left))
                        {
                            type = PlatformType.RightEdge;
                        }
                        else if (!IsGround(cell + Vector2Int.left) && !IsGround(cell + Vector2Int.right))
                        {
                            type = PlatformType.Solo;
                        }
                        PlatformCell platformCell = new PlatformCell(cell, type);
                        platforms.AddCell(platformCell);
                        Debug.Log("Adding platform cell: " + platformCell);
                    }
                }
            }
        }

        // Compute all possible movements for each platform cell
        // We will use the platform cells to compute the path
        void ComputeReachableNeighbours()
        {
            foreach (PlatformCell cell in platforms.cells.Values)
            {
                AddGround(cell);
                AddGroundBelow(cell);
                AddJumps(cell);
            }
        }

        private void AddGround(PlatformCell cell)
        {
            // Debug.Log("Adding ground to " + cell);
            if (cell.type == PlatformType.Ground)
            {
                // Debug.Log("Adding ground to " + cell + " with possible actions " + cell.possibleActions.ToString());
                cell.reachableNeighbours.Add(platforms[cell.position + Vector2Int.left]);
                cell.reachableNeighbours.Add(platforms[cell.position + Vector2Int.right]);
                cell.possibleActions.Add(PossibleActions.MoveLeft);
                cell.possibleActions.Add(PossibleActions.MoveRight);
            }
            if (cell.type == PlatformType.LeftEdge)
            {
                cell.reachableNeighbours.Add(platforms[cell.position + Vector2Int.right]);
                cell.possibleActions.Add(PossibleActions.MoveRight);
            }
            if (cell.type == PlatformType.RightEdge)
            {
                cell.reachableNeighbours.Add(platforms[cell.position + Vector2Int.left]);
                cell.possibleActions.Add(PossibleActions.MoveLeft);
            }
        }

        private void AddGroundBelow(PlatformCell cell)
        {
            if (cell.type == PlatformType.LeftEdge)
            {
                PlatformCell groundBelow = GroundBelow(cell.position + Vector2Int.left);
                if (! groundBelow.Equals(default(PlatformCell)))
                {
                    cell.reachableNeighbours.Add(groundBelow);
                    cell.possibleActions.Add(PossibleActions.FallLeft);
                }
            }
            if (cell.type == PlatformType.RightEdge)
            {
                PlatformCell groundBelow = GroundBelow(cell.position + Vector2Int.right);
                if (! groundBelow.Equals(default(PlatformCell)))
                {
                    cell.reachableNeighbours.Add(groundBelow);
                    cell.possibleActions.Add(PossibleActions.FallRight);
                }
            }
            if (cell.type == PlatformType.Solo)
            {
                PlatformCell groundBelow = GroundBelow(cell.position + Vector2Int.left);
                if (! groundBelow.Equals(default(PlatformCell)))
                {
                    cell.reachableNeighbours.Add(groundBelow);
                    cell.possibleActions.Add(PossibleActions.FallLeft);
                }
                groundBelow = GroundBelow(cell.position + Vector2Int.right);
                if (! groundBelow.Equals(default(PlatformCell)))
                {
                    cell.reachableNeighbours.Add(groundBelow);
                    cell.possibleActions.Add(PossibleActions.FallRight);
                }
            }
        }

        private void AddJumps(PlatformCell cell)
        {
            for (int length = 1; length <= 2; length++)
            {
                AddJump(cell, Vector2Int.left*length, 2);
                AddJump(cell, Vector2Int.right*length, 2);
            }
            AddJump(cell, Vector2Int.left*3, 1);
            AddJump(cell, Vector2Int.right*3, 1);
            AddJump(cell, Vector2Int.left*4, 0);
            AddJump(cell, Vector2Int.right*4, 0);
        }

        private void AddJump(PlatformCell cell, Vector2Int direction, int height = 1)
        {
            PossibleActions action = PossibleActions.JumpLeft;
            if (direction.x > 0)
            {
                action = PossibleActions.JumpRight;
            }
            Vector2Int jumpableCell = cell.position + Vector2Int.up*height + direction;
            if (platforms.Contains(jumpableCell))
            {
                cell.reachableNeighbours.Add(platforms[jumpableCell]);
                cell.possibleActions.Add(action);
                Debug.Log("Adding jump upwards to " + cell + " with possible actions " + action);
            }
            PlatformCell groundBelow = GroundBelow(jumpableCell);
            if (! groundBelow.Equals(default(PlatformCell)))
            {
                cell.reachableNeighbours.Add(groundBelow);
                cell.possibleActions.Add(action);
                Debug.Log("Adding jump downwards to " + cell + " with possible actions " + action);
            }
        }

        private PlatformCell GroundBelow(Vector2Int cell)
        {
            for (int y = cell.y - 1; y >= tilemap.cellBounds.yMin; y--)
            {
                if (platforms.Contains(new Vector2Int(cell.x, y)))
                {
                    return platforms[new Vector2Int(cell.x, y)];
                }
            }
            return default(PlatformCell);
        }

        private bool IsGround(Vector2Int cell)
        {
            return tilemap.HasTile((Vector3Int) cell + Vector3Int.down) && !tilemap.HasTile((Vector3Int) cell);
        }


        private void Move(Vector2 direction)
        {
            Vector2Int playerCell = (Vector2Int) tilemap.WorldToCell(player.transform.position);
            // Debug.Log("Moving from " + playerCell + " in direction " + direction + " with previous action " + previousAction);
            FrameInput = new FrameInput();
            if (platforms.Contains(playerCell))
            {
                PlatformCell cell = platforms[playerCell];
                Debug.Log("I am on a platform " + cell);
                if (cell.possibleActions.Contains(PossibleActions.MoveLeft) && direction == Vector2Int.left)
                {
                    FrameInput.Move = direction;
                    Debug.Log("I am moving left");              
                    previousAction = PossibleActions.MoveLeft;
                }
                else if (cell.possibleActions.Contains(PossibleActions.MoveRight) && direction == Vector2Int.right)
                {
                    FrameInput.Move = direction;
                    Debug.Log("I am moving right");
                    previousAction = PossibleActions.MoveRight;
                }
                else if (cell.possibleActions.Contains(PossibleActions.JumpRight) && direction == Vector2Int.right)
                {
                    FrameInput.Move = direction;
                    FrameInput.JumpDown = true;
                    FrameInput.JumpHeld = true;
                    Debug.Log("I am jumping right");
                    previousAction = PossibleActions.JumpRight;
                    startJumpCell = cell;
                }
                else if (cell.possibleActions.Contains(PossibleActions.JumpLeft) && direction == Vector2Int.left)
                {
                    FrameInput.Move = direction;
                    FrameInput.JumpDown = true;
                    FrameInput.JumpHeld = true;
                    Debug.Log("I am jumping left");
                    previousAction = PossibleActions.JumpLeft;
                    startJumpCell = cell;
                }
                else if (cell.possibleActions.Contains(PossibleActions.FallRight) && direction == Vector2Int.right)
                {
                    FrameInput.Move = direction;
                    Debug.Log("I am falling right");
                    previousAction = PossibleActions.FallRight;
                }
                else if (cell.possibleActions.Contains(PossibleActions.FallLeft) && direction == Vector2Int.left)
                {
                    FrameInput.Move = direction;
                    Debug.Log("I am falling left");
                    previousAction = PossibleActions.FallLeft;
                }
            }
            else if (previousAction == PossibleActions.JumpLeft) {
                FrameInput.Move = handleJumpMovement(playerCell, direction, previousAction, startJumpCell);
                // If I have ground below me, I stop jumping
                if (GroundBelow(playerCell).Equals(default(PlatformCell)))
                {
                    FrameInput.JumpHeld = true;
                    FrameInput.JumpDown = true;
                }
                else if (!GroundBelow(playerCell).Equals(startJumpCell)){
                    FrameInput.JumpHeld = false;
                    FrameInput.JumpDown = false;
                    FrameInput.Move = Vector2.zero;
                    previousAction = PossibleActions.FallLeft;
                }
            }
            else if (previousAction == PossibleActions.JumpRight) {
                FrameInput.Move = handleJumpMovement(playerCell, direction, previousAction, startJumpCell);
                // If I have ground below me, I stop jumping
                if (GroundBelow(playerCell).Equals(default(PlatformCell)))
                {
                    FrameInput.JumpHeld = true;
                    FrameInput.JumpDown = true;
                }
                else if (!GroundBelow(playerCell).Equals(startJumpCell)){
                    FrameInput.JumpHeld = false;
                    FrameInput.JumpDown = false;
                    FrameInput.Move = Vector2.zero;
                    previousAction = PossibleActions.FallRight;
                    Debug.Log("I stop jumping as I am above a platform "+ GroundBelow(playerCell));
                }
            }
			else if (previousAction == PossibleActions.FallLeft || previousAction == PossibleActions.FallRight)
			{
				FrameInput.Move = Vector2.zero;
			}
            else
            {
              	// Instead of being stuck, we move backwards
				// FrameInput.Move = -direction;
				FrameInput.Move = Vector2.zero;
            }
        }

		Vector2 handleJumpMovement(Vector2Int playerCell, Vector2 direction, PossibleActions previousAction, PlatformCell startJumpCell) {
			// I need to check the height of the jump to know if I can move right
			// And I need to adjust the movement to the right if I am below the platform
			PlatformCell highestCell = startJumpCell;
			foreach (PlatformCell jumpCell in startJumpCell.reachableNeighbours) {
				if (jumpCell.position.x < startJumpCell.position.x && direction == Vector2Int.right) {
					// Skip cells on the left
					continue;
				}
				if (jumpCell.position.x > startJumpCell.position.x && direction == Vector2Int.left) {
					// Skip cells on the right
					continue;
				}
				if (jumpCell.position.y > highestCell.position.y) {
					highestCell = jumpCell;
				}
			}
			if (highestCell.position.y >= playerCell.y) {
				// I am below the platform
				// I need to adjust my speed to the right not to hit the platform
				float distance = Vector2.Distance(playerCell, highestCell.position);
				if (distance < 2) {
					// I am close to the platform
					// I need to slow down
					Debug.Log("I am close to the platform");
					return direction * 0.5f;
				}
				Debug.Log("I am far to the platform");
				return direction;
			}
			Debug.Log("The platform is below me");
			return direction;
		}
    }
}