using UnityEngine;
using UnityEngine.UI;

public class SwingMechanic : MonoBehaviour
{
    [Header("Node References")]
    public Camera mainCamera;
    public Transform bodyPivot;
    public Transform clubShaft;
    public Transform ball;
    public Transform tee;
    public Transform flag;

    [Header("UI")]
    public Text shotLabel;
    public Slider powerBar;
    public Text instructionLabel;

    [Header("Swing Settings")]
    public float maxBackswingRotation = 85f;
    public float maxFollowthroughRotation = 100f;
    public float wristCockLag = 0.25f;
    public float maxDragDistance = 300f;

    [Header("Ball Flight")]
    public float gravity = 9.8f;
    public float maxLaunchSpeed = 65f;

    // State machine
    enum SwingState { Address, Backswing, Downswing, FollowThrough, BallFlight, Reset }
    SwingState state = SwingState.Address;

    // Input tracking
    Vector2 dragStartPos;
    Vector2 dragCurrentPos;
    bool isDragging;
    float backswingAmount;

    // Tempo tracking
    float backswingStartTime;
    float backswingEndTime;

    // Ball flight
    bool ballInFlight;
    Vector3 ballVelocity;
    Vector3 ballStartPos;
    float flightTime;

    // Camera
    Transform cameraTransform;
    Vector3 cameraOriginalPos;
    Quaternion cameraOriginalRot;
    bool cameraPanActive;
    float cameraPanTime;
    float cameraPanDuration = 1.2f;

    // Shot data
    string shotType = "";
    float shotPower;
    float shotAccuracy = 1f;

    // Animation
    bool animatingDownswing;
    float downswingTimer;
    float downswingDuration;
    float downswingPhase; // 0 = to impact, 1 = follow through
    float downswingStartRot;
    float clubStartRot;

    bool animatingReset;
    float resetTimer;
    float resetStartBodyRot;
    float resetStartClubRot;
    float resetStartCamX;

    // Reset timer after landing
    float landedTimer;
    bool waitingForReset;

    void Start()
    {
        cameraTransform = mainCamera.transform;
        cameraOriginalPos = cameraTransform.localPosition;
        cameraOriginalRot = cameraTransform.localRotation;
        ballStartPos = ball.localPosition;

        if (shotLabel != null) shotLabel.gameObject.SetActive(false);
        if (powerBar != null) powerBar.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleInput();

        if (animatingDownswing)
            UpdateDownswingAnimation();

        if (ballInFlight)
            UpdateBallFlight();

        if (cameraPanActive)
            UpdateCameraPan();

        if (animatingReset)
            UpdateResetAnimation();

        if (waitingForReset)
        {
            landedTimer -= Time.deltaTime;
            if (landedTimer <= 0f)
            {
                waitingForReset = false;
                StartReset();
            }
        }
    }

    // ── Input ──────────────────────────────────────────────────

    void HandleInput()
    {
        // Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                StartDrag(touch.position);
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                dragCurrentPos = touch.position;
                if (state == SwingState.Backswing) UpdateBackswing();
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                EndDrag(touch.position);
        }
        // Mouse input (for editor testing)
        else
        {
            if (Input.GetMouseButtonDown(0))
                StartDrag(Input.mousePosition);
            else if (Input.GetMouseButton(0) && isDragging)
            {
                dragCurrentPos = Input.mousePosition;
                if (state == SwingState.Backswing) UpdateBackswing();
            }
            else if (Input.GetMouseButtonUp(0))
                EndDrag(Input.mousePosition);
        }
    }

    void StartDrag(Vector2 pos)
    {
        if (state != SwingState.Address && state != SwingState.Reset) return;
        if (state == SwingState.Reset) InstantReset();

        isDragging = true;
        dragStartPos = pos;
        dragCurrentPos = pos;
        backswingStartTime = Time.time;
        state = SwingState.Backswing;

        if (shotLabel != null) shotLabel.gameObject.SetActive(false);
        if (powerBar != null) powerBar.gameObject.SetActive(false);
    }

    void EndDrag(Vector2 pos)
    {
        if (!isDragging) return;
        isDragging = false;
        dragCurrentPos = pos;

        if (state == SwingState.Backswing && backswingAmount > 0.15f)
        {
            backswingEndTime = Time.time;
            state = SwingState.Downswing;
            ExecuteDownswing();
        }
        else
        {
            StartReset();
        }
    }

    // ── Backswing ──────────────────────────────────────────────

    void UpdateBackswing()
    {
        float dragDelta = dragCurrentPos.x - dragStartPos.x;
        backswingAmount = Mathf.Clamp01(dragDelta / maxDragDistance);

        // Rotate body pivot right
        float bodyRot = backswingAmount * maxBackswingRotation;
        bodyPivot.localEulerAngles = new Vector3(0, bodyRot, 0);

        // Club lags behind
        float clubLag = backswingAmount * maxBackswingRotation * (1f - wristCockLag);
        clubShaft.localEulerAngles = new Vector3(0, clubLag - bodyRot, 0);

        // Update power bar
        if (powerBar != null)
        {
            powerBar.gameObject.SetActive(true);
            powerBar.value = backswingAmount;
        }
    }

    // ── Downswing ──────────────────────────────────────────────

    void ExecuteDownswing()
    {
        float backswingTime = backswingEndTime - backswingStartTime;
        ClassifyShot(backswingTime, backswingAmount);

        downswingDuration = GetDownswingDuration();
        downswingTimer = 0f;
        downswingPhase = 0f;
        downswingStartRot = bodyPivot.localEulerAngles.y;
        clubStartRot = clubShaft.localEulerAngles.y;
        animatingDownswing = true;
    }

    void ClassifyShot(float backswingTime, float power)
    {
        if (backswingTime < 0.25f && power > 0.7f)
        {
            shotType = "POWER DRIVE";
            shotPower = power * 1.15f;
            shotAccuracy = 0.75f;
        }
        else if (backswingTime < 0.5f && power > 0.5f)
        {
            shotType = "CLEAN STRIKE";
            shotPower = power * 1.0f;
            shotAccuracy = 1.0f;
        }
        else if (power < 0.45f)
        {
            shotType = "PUNCH SHOT";
            shotPower = power * 0.8f;
            shotAccuracy = 0.9f;
        }
        else if (backswingTime > 0.7f)
        {
            shotType = "SOFT LOB";
            shotPower = power * 0.65f;
            shotAccuracy = 0.85f;
        }
        else
        {
            shotType = "CLEAN STRIKE";
            shotPower = power * 0.95f;
            shotAccuracy = 0.95f;
        }
        shotPower = Mathf.Clamp(shotPower, 0.1f, 1.2f);
    }

    float GetDownswingDuration()
    {
        switch (shotType)
        {
            case "POWER DRIVE": return 0.3f;
            case "PUNCH SHOT": return 0.25f;
            case "SOFT LOB": return 0.55f;
            default: return 0.4f;
        }
    }

    void UpdateDownswingAnimation()
    {
        downswingTimer += Time.deltaTime;

        float impactTime = downswingDuration * 0.4f;
        float followTime = downswingDuration * 0.6f;

        if (downswingTimer <= impactTime)
        {
            // Phase 1: swing to impact (rotation back to 0)
            float t = EaseInOutCubic(downswingTimer / impactTime);
            float bodyY = Mathf.Lerp(downswingStartRot, 0f, t);
            float clubY = Mathf.Lerp(clubStartRot, 0f, t);
            bodyPivot.localEulerAngles = new Vector3(0, bodyY, 0);
            clubShaft.localEulerAngles = new Vector3(0, clubY, 0);
        }
        else if (downswingTimer <= impactTime + 0.001f + followTime)
        {
            // Trigger impact once
            if (downswingPhase < 1f)
            {
                downswingPhase = 1f;
                OnImpact();
            }

            // Phase 2: follow through
            float elapsed = downswingTimer - impactTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / followTime));
            float bodyY = Mathf.Lerp(0f, -maxFollowthroughRotation, t);
            float clubY = Mathf.Lerp(0f, -15f, t);
            bodyPivot.localEulerAngles = new Vector3(0, bodyY, 0);
            clubShaft.localEulerAngles = new Vector3(0, clubY, 0);
        }
        else
        {
            animatingDownswing = false;
            state = SwingState.BallFlight;
        }
    }

    // ── Impact ─────────────────────────────────────────────────

    void OnImpact()
    {
        state = SwingState.FollowThrough;

        // Launch ball
        ballInFlight = true;
        flightTime = 0f;
        ballStartPos = ball.localPosition;

        float speed = shotPower * maxLaunchSpeed;
        float angle = GetLaunchAngle();
        float lateralError = (1f - shotAccuracy) * Random.Range(-0.15f, 0.15f);

        ballVelocity = new Vector3(
            lateralError * speed,
            Mathf.Sin(angle * Mathf.Deg2Rad) * speed,
            -Mathf.Cos(angle * Mathf.Deg2Rad) * speed // negative Z = down fairway
        );

        // Start camera pan
        cameraPanActive = true;
        cameraPanTime = 0f;

        // Show shot type
        if (shotLabel != null)
        {
            shotLabel.text = shotType;
            shotLabel.gameObject.SetActive(true);
        }
        if (powerBar != null)
            powerBar.gameObject.SetActive(false);
    }

    float GetLaunchAngle()
    {
        switch (shotType)
        {
            case "POWER DRIVE": return 12f;
            case "PUNCH SHOT": return 8f;
            case "SOFT LOB": return 42f;
            default: return 18f;
        }
    }

    // ── Ball Flight ────────────────────────────────────────────

    void UpdateBallFlight()
    {
        flightTime += Time.deltaTime;
        ballVelocity.y -= gravity * Time.deltaTime;
        ball.localPosition += ballVelocity * Time.deltaTime;

        if (flightTime > 0.3f && ball.localPosition.y <= ballStartPos.y)
        {
            Vector3 pos = ball.localPosition;
            pos.y = ballStartPos.y;
            ball.localPosition = pos;
            ballInFlight = false;
            OnBallLanded();
        }
    }

    void OnBallLanded()
    {
        float dist = Vector3.Distance(ball.localPosition, ballStartPos) * 1.09f;
        if (shotLabel != null)
            shotLabel.text = shotType + "\n" + Mathf.Round(dist) + " yards";

        waitingForReset = true;
        landedTimer = 2.5f;
    }

    // ── Camera Pan ─────────────────────────────────────────────

    void UpdateCameraPan()
    {
        cameraPanTime += Time.deltaTime;
        float t = Mathf.Clamp01(cameraPanTime / cameraPanDuration);
        t = EaseOutCubic(t);

        float panAngle = Mathf.Lerp(0f, 35f, t); // tilt up
        Vector3 euler = cameraOriginalRot.eulerAngles;
        euler.x -= panAngle;
        cameraTransform.localRotation = Quaternion.Euler(euler);

        if (cameraPanTime > cameraPanDuration)
            cameraPanActive = false;
    }

    // ── Reset ──────────────────────────────────────────────────

    void StartReset()
    {
        state = SwingState.Reset;
        animatingReset = true;
        resetTimer = 0f;
        resetStartBodyRot = bodyPivot.localEulerAngles.y;
        if (resetStartBodyRot > 180f) resetStartBodyRot -= 360f;
        resetStartClubRot = clubShaft.localEulerAngles.y;
        if (resetStartClubRot > 180f) resetStartClubRot -= 360f;
        resetStartCamX = cameraTransform.localEulerAngles.x;
        if (resetStartCamX > 180f) resetStartCamX -= 360f;
    }

    void UpdateResetAnimation()
    {
        resetTimer += Time.deltaTime;
        float t = Mathf.Clamp01(resetTimer / 0.6f);
        t = EaseInOutQuad(t);

        bodyPivot.localEulerAngles = new Vector3(0, Mathf.Lerp(resetStartBodyRot, 0, t), 0);
        clubShaft.localEulerAngles = new Vector3(0, Mathf.Lerp(resetStartClubRot, 0, t), 0);
        cameraTransform.localRotation = Quaternion.Euler(
            Mathf.Lerp(resetStartCamX, cameraOriginalRot.eulerAngles.x, t),
            cameraOriginalRot.eulerAngles.y,
            cameraOriginalRot.eulerAngles.z
        );

        if (resetTimer >= 0.6f)
        {
            animatingReset = false;
            FinishReset();
        }
    }

    void FinishReset()
    {
        ball.localPosition = ballStartPos;
        ballInFlight = false;
        cameraTransform.localPosition = cameraOriginalPos;
        cameraTransform.localRotation = cameraOriginalRot;
        cameraPanActive = false;
        backswingAmount = 0f;
        bodyPivot.localEulerAngles = Vector3.zero;
        clubShaft.localEulerAngles = Vector3.zero;
        if (shotLabel != null) shotLabel.gameObject.SetActive(false);
        if (powerBar != null) powerBar.gameObject.SetActive(false);
        state = SwingState.Address;
    }

    void InstantReset()
    {
        bodyPivot.localEulerAngles = Vector3.zero;
        clubShaft.localEulerAngles = Vector3.zero;
        ball.localPosition = ballStartPos;
        cameraTransform.localPosition = cameraOriginalPos;
        cameraTransform.localRotation = cameraOriginalRot;
        ballInFlight = false;
        cameraPanActive = false;
        backswingAmount = 0f;
        animatingReset = false;
        animatingDownswing = false;
        state = SwingState.Address;
    }

    // ── Easing ─────────────────────────────────────────────────

    float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
