using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TarodevController;

namespace Automotion
{
    public class AStar : MonoBehaviour
    {
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] private GameObject target;
        private int maxJumpHeight = 2;
        private int MAX_LOOPS = 400000;
        private int MAX_PATH_SIZE = 1000;
        public bool CONNECTIVITY_8 = true;

        private float compuatationTime = 0f;

        // Code sample
        // void Start()
        // {
        //     StartCoroutine(Execute());
        // }
        // IEnumerator Execute() {
        //     if (target != null)
        //         FindPath((Vector2Int)obstacleTilemap.WorldToCell(transform.position), (Vector2Int)obstacleTilemap.WorldToCell(target.transform.position));
        //     yield return new WaitForSeconds(1);
        //     StartCoroutine(Execute());
        // }

        public Path FindPath(Vector2Int start, Vector2Int end, Tilemap obstacleTilemap, int maxJumpHeight = 2)         
        {
            this.maxJumpHeight = maxJumpHeight;
            this.obstacleTilemap = obstacleTilemap;
            compuatationTime = Time.realtimeSinceStartup;
            // Debug.Log("Finding path from " + start + " to " + end);
            int maxLoops = 0;
            List<GraphNode> openList = new List<GraphNode> {CreateGraphNode(start)};
            List<Vector2> closedList = new List<Vector2>();
            Dictionary<Vector2, GraphNode> parentList = new Dictionary<Vector2, GraphNode>();
            Dictionary<Vector2, int> gScore = new Dictionary<Vector2, int> {{start, 0}};
            Dictionary<Vector2, int> fScore = new Dictionary<Vector2, int> {{start, Heuristic(start, end)}};

            while (openList.Count > 0 && maxLoops++ < MAX_LOOPS)
            {
                GraphNode current = GetLowestFScore(openList, fScore);
                // Debug.Log("Current node: " + current);
                if (Heuristic(current.position, end) <= 0.5)
                {
                    return ReconstructPath(parentList, current);
                }

                openList.Remove(current);
                closedList.Add(current.position);

                foreach (GraphNode neighbor in GetNeighbors(current))
                {
                    // Debug.Log("Neighbor: " + neighbor);
                    if (closedList.Contains(neighbor.position))
                    {
                        continue;
                    }
                    int tentativeGScore = gScore[current.position] + neighbor.jumpScore + 1;
                    if (!openList.Contains(neighbor) || tentativeGScore < gScore[neighbor.position])
                    {
                        parentList[neighbor.position] = current;
                        gScore[neighbor.position] = tentativeGScore;
                        fScore[neighbor.position] = gScore[neighbor.position] + Heuristic(neighbor.position, end);
                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                            // Debug.Log("AddNeighbor: " + neighbor);
                        }
                        else {
                            openList.Remove(neighbor);
                            openList.Add(neighbor);
                            // Debug.Log("Neighbor already in openList: " + neighbor);
                        }
                    }
                }
            }
            return new Path();
        }

        private Path ReconstructPath(Dictionary<Vector2, GraphNode> parentList, GraphNode current)
        {
            Path path = new Path();
            path.path = new List<GraphNode> {};
            int max = MAX_PATH_SIZE;
            path.Insert(0, current);
            while (parentList.ContainsKey(current.position) && max-- > 0)
            {
                current = parentList[current.position];
                path.Insert(0, current);
                // Debug.Log("Adding node to path" + current+ " Path: " + path.path.Count + " nodes");
            }
            // Remove the first node because it's the start position
            // It leads to a bug when the player has passed the target position
            // And so is willing to go back to the start position
            path.path.RemoveAt(0);
            compuatationTime = Time.realtimeSinceStartup - compuatationTime;
            // Debug.Log("[AStar] Path found in " + compuatationTime + "ms" + " Path: " + path.path.Count + " nodes");
            return path;
        }

        private GraphNode GetLowestFScore(List<GraphNode> openList, Dictionary<Vector2, int> fScore)
        {
            GraphNode lowest = openList[0];
            foreach (GraphNode node in openList)
            {
                if (fScore.ContainsKey(node.position)) {
                    if (fScore[node.position] < fScore[lowest.position])
                    {
                        lowest = node;
                    }
                }
            }

            return lowest;
        }

        private int Heuristic(Vector2 a, Vector2 b)
        {
            return (int)(Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y));
        }

        private List<GraphNode> GetNeighbors(GraphNode cell)
        {
            List<GraphNode> neighbours = new List<GraphNode>();
            GraphNode potentialNeighbour;
            potentialNeighbour = CreateGraphNode(cell, Vector2Int.up);
            if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
            potentialNeighbour = CreateGraphNode(cell, Vector2Int.down);
            if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
            potentialNeighbour = CreateGraphNode(cell, Vector2Int.left);
            if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
            potentialNeighbour = CreateGraphNode(cell, Vector2Int.right);
            if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
            if (CONNECTIVITY_8) {
                potentialNeighbour = CreateGraphNode(cell, new Vector2Int(-1, 1));
                if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
                potentialNeighbour = CreateGraphNode(cell, new Vector2Int(1, 1));
                if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
                potentialNeighbour = CreateGraphNode(cell, new Vector2Int(-1, -1));
                if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
                potentialNeighbour = CreateGraphNode(cell, new Vector2Int(1, -1));
                if (!potentialNeighbour.Equals(GraphNode.NONE)) neighbours.Add(potentialNeighbour);
            }
            return neighbours;
        }

        private bool InBounds(Vector2 position)
        {
            return position.x >= obstacleTilemap.cellBounds.xMin && position.x <= obstacleTilemap.cellBounds.xMax &&
                   position.y >= obstacleTilemap.cellBounds.yMin && position.y <= obstacleTilemap.cellBounds.yMax;
        }

        private GraphNode CreateGraphNode(Vector2Int position)
        {
            return CreateGraphNode(new GraphNode(position,0), Vector2.zero);
        }

        private GraphNode CreateGraphNode(GraphNode cell, Vector2 direction)
        {
            // If the tile is occupied, I can't move there
            Vector3Int nextCellPosition = new Vector3Int((int)(cell.position.x+direction.x), (int)(cell.position.y+direction.y), 0);
            if (obstacleTilemap.HasTile(nextCellPosition) || !InBounds(cell.position + direction) || 
                Physics2D.Raycast(cell.position, direction, 1, obstacleTilemap.gameObject.layer).collider != null)
                // Physics2D.CapsuleCast(cell.position, direction, 1, LayerMask.GetMask("Ground")).collider != null)
            {
                return GraphNode.NONE;
            }
            if (IsGround(Vector2Int.FloorToInt(cell.position + direction)))
            {
                return new GraphNode(cell.position + direction, 0);
            }
            bool startFall = cell.startFall;
            if (direction.y < 0) {
                startFall = true;
            }
            // When jumpscore has reached the max height
            if (cell.jumpScore >= maxJumpHeight*2)
            {
                startFall = true;
            }
            // When I move twice front, I start falling
            if (direction.x != 0 && cell.jumpScore%2==1) {
                startFall = true;
            }
            if (startFall) {
                // I cannot go higher
                if (direction.y > 0) return GraphNode.NONE;
                // I can go on a side if jumpScore is odd
                if (direction.x != 0 && direction.y == 0) {
                    if (cell.jumpScore%2 == 1) return new GraphNode(cell.position + direction, cell.jumpScore + 1, startFall);
                    else return GraphNode.NONE;
                }
                else if (direction.x != 0 && direction.y < 0) {
                    return new GraphNode(cell.position + direction, cell.jumpScore + 2, startFall);
                }
                // I can go down if jumpScore is even
                if (cell.jumpScore%2 == 0) return new GraphNode(cell.position + direction, cell.jumpScore + 2, startFall);
                return new GraphNode(cell.position + direction, cell.jumpScore + 1, startFall);
            }
            // If I don't fall, I can go up if jumpScore is even
            if (direction.y != 0 && direction.x == 0) {
                if (cell.jumpScore%2 == 0) return new GraphNode(cell.position + direction, cell.jumpScore + 2, startFall);
                return new GraphNode(cell.position + direction, cell.jumpScore + 1, startFall);
            }
            else if (direction.y != 0 && direction.x != 0) {
                return new GraphNode(cell.position + direction, cell.jumpScore + 2, startFall);
            }
            return new GraphNode(cell.position + direction, cell.jumpScore + 1, startFall);
        }

        private bool IsGround(Vector2Int cell)
        {
            // Can be achieved with raycast but I think this is faster
            return obstacleTilemap.HasTile((Vector3Int) cell + Vector3Int.down) && !obstacleTilemap.HasTile((Vector3Int) cell);
        }

    }

    public struct GraphNode
    {
        public static GraphNode NONE = new GraphNode(Vector2.zero, -1);
        public Vector2 position;
        public int jumpScore;
        public bool startFall;

        public GraphNode(Vector2 position, int jumpScore = 0, bool startFall = false)
        {
            this.position = position;
            this.jumpScore = jumpScore;
            this.startFall = startFall;
        }
        public bool Equals(GraphNode other)
        {
            return position.Equals(other.position);
        }
    
        public override int GetHashCode()
        {
            return position.GetHashCode();
        }

        public override string ToString()
        {
            return position + " jump: " + jumpScore + " falling: " + startFall;
        }
    }
    
    public struct Path
    {
        public static Vector2 SHIFT = new Vector2(0.5f, 0.5f);
        public List<GraphNode> path;
        public void Insert(int index, GraphNode node)
        {
            node.position += SHIFT;
            path.Insert(index, node);
        }
    }
}