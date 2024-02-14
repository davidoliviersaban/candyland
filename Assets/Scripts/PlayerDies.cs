using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Automotion {

    [RequireComponent(typeof(GameObject))]
    public class PlayerDies : MonoBehaviour
    {

        private Vector2 startingPlayerPos;
        private float DYING_HEIGHT = -40f;

        // Start is called before the first frame update
        void Start()
        {
            startingPlayerPos = gameObject.transform.position;
            DYING_HEIGHT = GameObject.Find("Tilemap").GetComponent<Tilemap>().cellBounds.yMin - 1;
        }

        // Update is called once per frame
        void Update()
        {
            if (transform.position.y < DYING_HEIGHT)
            {
                //GameObject oldGameObject = gameObject;
                gameObject.transform.position = startingPlayerPos;
                EnemyMovement enemyMovement = this.GetComponent<EnemyMovement>();
                if (enemyMovement != null) {
                    enemyMovement.Reset();
                }
            }
        }
    }
}