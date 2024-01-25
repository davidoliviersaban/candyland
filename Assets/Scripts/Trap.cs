using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Traps : MonoBehaviour
{

    [SerializeField]
    public GameObject player;
    [SerializeField]
    public Tilemap trapMap;
    private SpriteRenderer sprite;
    private Material originalMaterial;
    [SerializeField]
    public Material blinkMaterial;
    public float blinkTime = 0.1f;
    private bool isBlinking = false;
    

    // Start is called before the first frame update
    void Start()
    {
        sprite = player.GetComponent<SpriteRenderer>();
        originalMaterial = sprite.material;
    }

    // Update is called once per frame
    void Update()
    {
        if (onTrap() && !isBlinking)
        {
            Debug.Log("Trap");
            isBlinking = true;
            InvokeRepeating("blink", 0, blinkTime);
        }
        else if (!onTrap() && isBlinking)
        {
            CancelInvoke("blink");
            resetBlink();
        }
    }

    bool onTrap()
    {
        Vector3Int cell = trapMap.WorldToCell(player.transform.position);
        if (trapMap.HasTile(cell))
        {
            return true;
        }
        return false;
    }

    void blink()
    {
        if (sprite.material == originalMaterial)
        {
            sprite.material = blinkMaterial;
        }
        else
        {
            sprite.material = originalMaterial;
        }
    }

    void resetBlink()
    {
        sprite.material = originalMaterial;
        isBlinking = false;
    }

}
