using UnityEngine;
using System.Collections.Generic;

public class BoatCutscene : MonoBehaviour
{
    [Header("Movement Path")]
    public List<Transform> waypoints = new List<Transform>();
    public float movementSpeed = 3.0f;
    public float arrivalDistance = 0.5f;

    [Header("Cinematic Camera Angles")]
    public Transform cameraPosition1; // Wide Angle (e.g., High in the sky looking at boat)
    public Transform cameraPosition2; // Character Close-up (usually parented to the boat!)
    public Transform cameraPosition3; // Shore View (stationed on the island shore looking out)

    [Header("Main Camera Reference")]
    public Transform mainCameraTransform;

    [Header("Camera Transition Speeds")]
    public float camLerpSpeed = 2.0f;

    [Header("Failsafe Settings")]
    public float cutsceneTimeout = 35.0f; // Force end cutscene after 35 seconds as a failsafe
    private float cutsceneTimer = 0f;

    private Vector3 startPosition;
    private float totalJourneyDistance;
    private Vector3 finalWaypointPosition;
    private int currentWaypointIndex = 0;
    private bool isCutscenePlaying = false;
    private GameObject spawnedPlayer;

    void Start()
    {
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    public void StartCutscene(GameObject player)
    {
        spawnedPlayer = player;
        currentWaypointIndex = 0;
        isCutscenePlaying = true;
        startPosition = transform.position;
        cutsceneTimer = 0f;

        if (waypoints.Count > 0)
        {
            Transform lastWaypoint = waypoints[waypoints.Count - 1];
            if (lastWaypoint != null)
            {
                finalWaypointPosition = lastWaypoint.position;
                totalJourneyDistance = Vector3.Distance(startPosition, finalWaypointPosition);
            }
            Debug.Log($"[BoatCutscene] Cutscene started with {waypoints.Count} waypoints. Failsafe timeout set to {cutsceneTimeout}s.");
        }
        else
        {
            Debug.LogWarning("Please configure Waypoints for the Boat Cutscene in Inspector!");
        }
    }

    void Update()
    {
        if (!isCutscenePlaying) return;

        // Failsafe timer
        cutsceneTimer += Time.deltaTime;
        if (cutsceneTimer >= cutsceneTimeout)
        {
            Debug.LogWarning($"[BoatCutscene] Cutscene exceeded timeout failsafe of {cutsceneTimeout} seconds! Forcing completion to ensure gameplay starts.");
            EndCutscene();
            return;
        }

        // 1. Boat Movement
        MoveBoat();

        // 2. Camera Cutscene Angles
        UpdateCinematicCamera();
    }

    private void MoveBoat()
    {
        if (waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
        {
            EndCutscene();
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        // Move towards target waypoint
        Vector3 targetPos = targetWaypoint.position;
        // Keep boat on water level
        targetPos.y = transform.position.y; 

        transform.position = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.deltaTime);

        // Rotate boat to face target waypoint smoothly
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Check if arrived at waypoint
        if (Vector3.Distance(transform.position, targetPos) < arrivalDistance)
        {
            Debug.Log($"[BoatCutscene] Arrived at waypoint {currentWaypointIndex + 1}/{waypoints.Count}: {targetWaypoint.name}");
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                EndCutscene();
            }
        }
    }

    private void UpdateCinematicCamera()
    {
        if (mainCameraTransform == null || waypoints.Count == 0) return;

        // Calculate continuous progress based on actual distance to final destination
        float progress = 0f;
        if (totalJourneyDistance > 0.1f)
        {
            float currentDist = Vector3.Distance(transform.position, finalWaypointPosition);
            progress = 1f - (currentDist / totalJourneyDistance);
            progress = Mathf.Clamp01(progress);
        }

        Transform targetCamTransform = cameraPosition1;

        // Determine camera target based on progress
        if (progress < 0.35f)
        {
            // Stage 1: Wide Angle Intro
            targetCamTransform = cameraPosition1;
        }
        else if (progress < 0.70f)
        {
            // Stage 2: Character Close-up
            targetCamTransform = cameraPosition2;
        }
        else
        {
            // Stage 3: Island Welcoming View
            targetCamTransform = cameraPosition3;
        }

        // Interpolate camera position and rotation smoothly
        if (targetCamTransform != null)
        {
            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetCamTransform.position, camLerpSpeed * Time.deltaTime);
            mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetCamTransform.rotation, camLerpSpeed * Time.deltaTime);
        }
    }

    private void EndCutscene()
    {
        if (!isCutscenePlaying) return; // Prevent double trigger
        isCutscenePlaying = false;
        Debug.Log("[BoatCutscene] Boat Cutscene Completed!");
        
        // Notify GameManager to start gameplay!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBoatArrived();
        }
        else
        {
            Debug.LogWarning("[BoatCutscene] GameManager.Instance is NULL! Attempting to find GameManager dynamically...");
            GameManager gm = GameObject.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.OnBoatArrived();
            }
            else
            {
                Debug.LogError("[BoatCutscene] Critical: Could not find GameManager in scene to end cutscene!");
            }
        }
    }
}
