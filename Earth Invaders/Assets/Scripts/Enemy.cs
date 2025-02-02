﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int lives = 3;
    [SerializeField] private float newAlienDelay = 5f;
    [SerializeField] private float maxRandomRunAmount = 0.5f;
    [SerializeField] private float maxRandomTargetAmount = 0.75f;
    [SerializeField] private float maxRandomShootAmount = 1f;
    
    [Header("Enemy Move/Shoot Properties: ")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float padding = 0.5f;
    [SerializeField] private float initialLagBehindOffset = 0f;
    [SerializeField] private float finalLagBehindOffset = 4f;
    [SerializeField] private float laserSpeed = 2f;
    [SerializeField] private float shootDelay = 1f;
    [SerializeField] private bool isShooting = false;
    
    [Header("Enemy Game Objects: ")]
    [SerializeField] private GameObject firstShip;
    [SerializeField] private GameObject secondShip;
    [SerializeField] private GameObject healthText;
    [SerializeField] private GameObject laserPrefab;
    
    private LaserDetector laserDetector;
    private AlienController alienController;
    private GameObject currentAlien;
    private Coroutine avoidingTimer;
    private LevelController levelController;
    
    [Header("Running Properties: ")]
    [SerializeField] private float verticalRunLaserRange = 1f;
    [SerializeField] private float horizontalRunLaserRange = 0.5f;
    [SerializeField] private float runDelay = 1f;
    [SerializeField] private float safeDistanceDown = -1f;
    
    bool running = false;
    private Coroutine runningTimer;
    private bool runningTimerRunning = false;
    private bool startRunTimer = false;
    private float randomRunAmount = 0f;

    [Header("Avoiding Properties: ")]
    [SerializeField] private float verticalAvoidLaserRange = 2f;
    [SerializeField] private float horizontalAvoidLaserRange = 2f;
    [SerializeField] private float avoidDelay = 1f;
    
    private bool avoiding = false;
    private bool avoidingTimerRunning = false;
    private bool startAvoidTimer = false;
    
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;
    
    private bool currentlyShooting = false;
    private int initialAlienCount = 0;
    private float lagBehindOffset;
    private bool findingNewRandoms = false;
    private float randomShootAmount = 0f;
    private float randomTargetAmount = 0f;
    

    void Start()
    {
        SetupMoveBoundaries();
        laserDetector = FindObjectOfType<LaserDetector>();
        alienController = FindObjectOfType<AlienController>();
        levelController = FindObjectOfType<LevelController>();
        lagBehindOffset = initialLagBehindOffset;
    }

    void Update()
    {
        if (initialAlienCount == 0)
        {
            initialAlienCount = alienController.GetAlienCount();
        }
        if (alienController.GetAlienCount() < initialAlienCount / 2)
        {
            lagBehindOffset = finalLagBehindOffset;
        }
        if (!currentlyShooting && isShooting && alienController.GetAlienCount() > 0)
        {
            StartCoroutine(Shoot());
        }

        if (!findingNewRandoms)
        {
            
            StartCoroutine(FindNewRandoms());
        }

        if (laserDetector.GetLasers().Count > 2)
        {
            RunFromLasers();
        }

        if (!running)
        {
            //AvoidLasers();
            if (!avoiding && alienController.GetAlienCount() > 0)
            {
                //TargetEnemy();
                TargetRandomEnemy();
            }
        }
    }
    private void SetupMoveBoundaries()
    {
        Camera camera = Camera.main;
        xMin = camera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x + padding;
        xMax = camera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x - padding;
        yMin = camera.ViewportToWorldPoint(new Vector3(0, 0, 0)).y + padding;
        yMax = camera.ViewportToWorldPoint(new Vector3(0, 1, 0)).y - padding;
    }

    private IEnumerator Shoot()
    {
        currentlyShooting = true;
        Vector3 laserPosition = new Vector3(transform.position.x,transform.position.y + 0.2f, transform.position.z);
        GameObject laser = Instantiate(laserPrefab,laserPosition,Quaternion.identity);
        laser.GetComponent<Rigidbody2D>().velocity = Vector2.up * laserSpeed;
        laser.tag = "Enemy Bullet";
        yield return new WaitForSeconds(shootDelay + randomShootAmount);
        currentlyShooting = false;
    }

    private void RunFromLasers()
    {
        List<GameObject> closeLasers = new List<GameObject>();
        
        foreach (var laser in laserDetector.GetLasers())
        {
            float distanceBetween = laser.transform.position.y - transform.position.y;
            if (distanceBetween < verticalRunLaserRange && distanceBetween > safeDistanceDown)
            {
                closeLasers.Add(laser);
            }
        }

        int closestIndex = 0;
        float closestDistance = 1000f;
        bool doneRunning = true;
        
        for (int i = 0; i < closeLasers.Count; i++)
        {
            float distance = Mathf.Abs(closeLasers[i].transform.position.x - transform.position.x);
            if (distance < closestDistance)
            {
                if (distance < horizontalRunLaserRange + randomRunAmount)
                {
                    running = true;
                    doneRunning = false;
                    startRunTimer = true;
                    closestIndex = i;
                }
            }
        }
        

        if (Mathf.Abs(closeLasers[closestIndex].transform.position.x - transform.position.x) > horizontalRunLaserRange + randomRunAmount)
        {
            doneRunning = true;
            if (startRunTimer)
            {
                startRunTimer = false;
                if (runningTimerRunning)
                {
                    StopCoroutine(runningTimer);
                    runningTimer = StartCoroutine(RunningTimer());
                }
                else
                {
                    runningTimer = StartCoroutine(RunningTimer());
                }
            }
        }
        if (closeLasers.Count > 2 && !doneRunning)
        {
            float avoidPosition = closeLasers[closestIndex].transform.position.x;
            float newPosition = Mathf.Clamp(Mathf.MoveTowards(transform.position.x, avoidPosition, -moveSpeed * Time.deltaTime),xMin,xMax);
            transform.position = new Vector2(newPosition, transform.position.y);
        }
    }

    private void AvoidLasers()
    {
        List<GameObject> closeLasers = new List<GameObject>();
        foreach (var laser in laserDetector.GetLasers())
        {
            float distanceBetween = laser.transform.position.y - transform.position.y;
            if (distanceBetween < verticalAvoidLaserRange && distanceBetween > safeDistanceDown) ;
            {
                closeLasers.Add(laser);
            }
        }

        var ordered = closeLasers.OrderBy(gameObject => gameObject.transform.position.x);
        List<GameObject> orderedLasers = new List<GameObject>();
        foreach (GameObject laser in ordered)
        {
            orderedLasers.Add(laser);
        }

        int safeIndex = 1;
        float safestDistance = 0f;
        for (int i = 1; i < orderedLasers.Count(); i++)
        {
            if (orderedLasers[i].transform.position.x - orderedLasers[i - 1].transform.position.x > safestDistance)
            {
                safestDistance = orderedLasers[i].transform.position.x - orderedLasers[i - 1].transform.position.x;
                safeIndex = i;
            }
        }

        int closestIndex = 0;
        float closestDistance = 1000f;
        bool laserClose = false;
        
        for (int i = 0; i < closeLasers.Count; i++)
        {
            float distance = Mathf.Abs(closeLasers[i].transform.position.x - transform.position.x);
            if (distance < closestDistance)
            {
                closestIndex = i;
                closestDistance = distance;
                if (distance < horizontalAvoidLaserRange)
                {
                    laserClose = true;
                }
                else
                {
                    laserClose = false;
                    startAvoidTimer = true;
                }
            }
        }
        if (!laserClose)
        {
            if (startAvoidTimer)
            {
                startAvoidTimer = false;
                if (avoidingTimerRunning)
                {
                    StopCoroutine(avoidingTimer);
                    avoidingTimer = StartCoroutine(AvoidingTimer());
                }
                else
                {
                    avoidingTimer = StartCoroutine(AvoidingTimer());
                }
            }
        }
        if (closeLasers.Count > 2 && laserClose)
        {
            
            float finalPosition = (orderedLasers[safeIndex].transform.position.x + orderedLasers[safeIndex - 1].transform.position.x) / 2f;
            float newPosition = Mathf.MoveTowards(transform.position.x, finalPosition, moveSpeed * Time.deltaTime);
            transform.position = new Vector2(newPosition, transform.position.y);
        }
        else
        {
            avoiding = false;
        }
    }

    private void TargetEnemy()
    {
        if (alienController.GetAlienCount() > 0)
        {
            float targetPosition = alienController.GetAverageAlienPosition() + (alienController.GetMoveSpeed() * lagBehindOffset);
            float newPosition = Mathf.Clamp(Mathf.MoveTowards(transform.position.x, targetPosition, moveSpeed * Time.deltaTime),xMin,xMax);
            transform.position = new Vector2(newPosition, transform.position.y);
        }
    }

    private void TargetRandomEnemy()
    {
        if (alienController.GetAlienCount() > 0 && currentAlien)
        {
            float targetPosition = currentAlien.transform.position.x + randomTargetAmount;
            float newPosition = Mathf.Clamp(Mathf.MoveTowards(transform.position.x, targetPosition, moveSpeed * Time.deltaTime), xMin, xMax);
            transform.position = new Vector2(newPosition, transform.position.y);
        }
    }

    private IEnumerator RunningTimer()
    {
        runningTimerRunning = true;
        yield return new WaitForSeconds(runDelay);
        running = false;
        runningTimerRunning = false;
    }
    
    private IEnumerator AvoidingTimer()
    {
        avoidingTimerRunning = true;
        yield return new WaitForSeconds(avoidDelay);
        avoiding = false;
        avoidingTimerRunning = false;
    }

    private IEnumerator FindNewRandoms()
    {
        findingNewRandoms = true;
        if (alienController.GetAlienCount() > 0)
        {
            int randomAlienNumber = Random.Range(0, alienController.GetAlienCount() - 1);
            currentAlien = alienController.GetAlienList()[randomAlienNumber];
        }

        randomRunAmount = Random.Range(0, maxRandomRunAmount);
        randomShootAmount = Random.Range(0, maxRandomShootAmount);
        randomTargetAmount = Random.Range(-randomTargetAmount, randomTargetAmount);
        yield return new WaitForSeconds(newAlienDelay);
        findingNewRandoms = false;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Alien Laser"))
        {
            laserDetector.RemoveLaser(other.gameObject);
            lives--;
            switch (lives)
            {
                case 2:
                    secondShip.SetActive(false);
                    healthText.GetComponent<Text>().text = "2";
                    break;
                case 1:
                    firstShip.SetActive(false);
                    healthText.GetComponent<Text>().text = "1";
                    break;
                case 0:
                    healthText.GetComponent<Text>().text = "0";
                    levelController.GameWon();
                    break;
            }
        }
    }
}
