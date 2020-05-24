﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alien : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float segmentsPerUnityUnit = 4f;
    [SerializeField] private float padding = 0.5f;
    [SerializeField] Sprite[] sprites = new Sprite[2];
    private int currentSprite = -1;
    private Vector3 unroundedPos;
    private Color color;
    
    void Start()
    {
        tag = "Alien";
        color = GetComponent<SpriteRenderer>().color;
        unroundedPos = transform.position;
        segmentsPerUnityUnit = GetComponentInParent<AlienController>().GetSegmentsPerUnityUnit();
    }

    private void Update()
    {
        //Move();
    }

    private void OnMouseDown()
    {
        if (!GetComponent<PlayerController>())
        {
            PlayerController playerController = gameObject.AddComponent<PlayerController>();
            playerController.SetMovementSpeed(movementSpeed);
            playerController.SetSegmentsPerUnityUnit(segmentsPerUnityUnit);
            playerController.SetPadding(padding);
            FindObjectOfType<LevelController>().AddNewPlayer(gameObject);
        }
    }

    public void Move(Vector3 moveBy)
    {
        unroundedPos += moveBy;

        var newX = Mathf.RoundToInt(unroundedPos.x * segmentsPerUnityUnit) / segmentsPerUnityUnit;
        var newY = Mathf.RoundToInt(unroundedPos.y * segmentsPerUnityUnit) / segmentsPerUnityUnit;

        if (CompareTag("Alien"))
        {
            transform.position = new Vector2(newX, newY);
        }
    }

    public Sprite[] GetSprites()
    {
        return sprites;
    }

    public void SwitchSprite()
    {
        currentSprite = currentSprite * -1;
        if (CompareTag("Alien"))
        {
            GetComponent<SpriteRenderer>().sprite = sprites[(currentSprite + 1) / 2];
        }
    }

    public void RemovePlayer()
    {
        Destroy(GetComponent<PlayerController>());
        tag = "Alien";
        GetComponent<SpriteRenderer>().color = color;
        Move(new Vector3(0,0,0));
    }
}
