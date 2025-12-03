using UnityEngine;
using System.Collections.Generic;

public class FightingGameCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform enemy;
    
    [Header("Movement Settings")]
    public Vector3 offset = new Vector3(0, 2.0f, -10f); // Adjust Y to keep ground in view
    public float smoothTime = 0.5f; // Lower = Snappier, Higher = Smoother
    
    [Header("Zoom Settings")]
    public float minZoom = 5f;  // Closest zoom (Orthographic Size)
    public float maxZoom = 12f; // Furthest zoom
    public float zoomLimiter = 50f; // Divisor for zoom calculation

    private Vector3 velocity;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Auto-find targets if empty
        if (player == null) player = GameObject.Find("Satoru Gojo").transform;
        if (enemy == null) 
        {
            GameObject e = GameObject.FindGameObjectWithTag("Enemy");
            if(e != null) enemy = e.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null || enemy == null) return;

        // 1. Move Camera
        Move();

        // 2. Zoom Camera
        Zoom();
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 newPosition = centerPoint + offset;

        // Smoothly move from current pos to new pos
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    void Zoom()
    {
        // Calculate new Orthographic size based on distance
        float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);
        
        // Smoothly apply zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime);
    }

    Vector3 GetCenterPoint()
    {
        // Returns the midpoint between Player and Enemy
        return (player.position + enemy.position) / 2f;
    }

    float GetGreatestDistance()
    {
        // Returns distance (X axis usually matters most in 2D fighters)
        return Vector3.Distance(player.position, enemy.position);
    }
}