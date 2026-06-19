using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PlayerLook : MonoBehaviour
{
    private float xRotation = 0;
    public Camera cam;
    public Light spotLight;
    public float xSensitivity = 2.0f;
    public float ySensitivity = 2.0f;

    [Header("Crouch Camera")]
    public float standCameraHeight = 1.2f;
    public float crouchCameraHeight = 0.6f;
    public float crouchCamLerpSpeed = 10f;

    [Header("Head Bob")]
    public float bobFrequency = 10f;
    public float bobHorizontalAmplitude = 0.05f;
    public float bobVerticalAmplitude = 0.08f;
    public float sprintBobMultiplier = 1.5f;

    private float bobTimer = 0f;
    private Vector3 defaultCamLocalPos;
    private PlayerMotor motor;

    [Header("Aim / Zoom")]
    public float aimFOV = 40f;
    public float aimLerpSpeed = 10f;
    private float defaultFOV = 80f;

    [Header("Crosshair")]
    public RectTransform crosshairTop;
    public RectTransform crosshairBottom;
    public RectTransform crosshairLeft;
    public RectTransform crosshairRight;

    [Header("Crosshair Settings")]
    public float normalSpread = 0f;
    public float aimSpread = 10f;
    public float crosshairLerpSpeed = 15f;

    private float currentCrosshairSpread = 0f;
    private Vector2 topStartPos, bottomStartPos, leftStartPos, rightStartPos;

    private void Start()
    {
        motor = GetComponent<PlayerMotor>();

        defaultCamLocalPos = new Vector3(
            cam.transform.localPosition.x,
            standCameraHeight,
            cam.transform.localPosition.z
        );

        cam.transform.localPosition = defaultCamLocalPos;

        if (crosshairTop != null) topStartPos = crosshairTop.anchoredPosition;
        if (crosshairBottom != null) bottomStartPos = crosshairBottom.anchoredPosition;
        if (crosshairLeft != null) leftStartPos = crosshairLeft.anchoredPosition;
        if (crosshairRight != null) rightStartPos = crosshairRight.anchoredPosition;
    }

    private void UpdateCameraHeight()
    {
        if (!motor) return;

        float targetY = motor.IsCrouching()
            ? crouchCameraHeight
            : standCameraHeight;

        Vector3 targetPos = new Vector3(
            defaultCamLocalPos.x,
            targetY,
            defaultCamLocalPos.z
        );

        defaultCamLocalPos = Vector3.Lerp(
            defaultCamLocalPos,
            targetPos,
            Time.deltaTime * crouchCamLerpSpeed
        );
    }

    public void ProccessLook(Vector2 input)
    {
        HandleZoom();
        UpdateCameraHeight();
        UpdateCrosshairSpread();

        float mouseX = input.x * xSensitivity * Time.deltaTime;
        float mouseY = input.y * ySensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        spotLight.transform.localRotation = Quaternion.Slerp(
            spotLight.transform.localRotation,
            cam.transform.localRotation,
            Time.deltaTime * 12f
        );
        transform.Rotate(Vector3.up * mouseX);
        UpdateHeadBob();
    }

    private void UpdateHeadBob()
    {
        if (!motor || !motor.isGrounded)
        {
            ResetCameraPosition();
            return;
        }

        if (!motor.isMoving)
        {
            ResetCameraPosition();
            return;
        }

        float frequency = bobFrequency;
        float horizontal = bobHorizontalAmplitude;
        float vertical = bobVerticalAmplitude;

        if (motor.IsSprinting())
        {
            frequency *= sprintBobMultiplier;
            horizontal *= sprintBobMultiplier;
            vertical *= sprintBobMultiplier;
        }

        bobTimer += Time.deltaTime * frequency;
        float x = Mathf.Cos(bobTimer) * horizontal;
        float y = Mathf.Abs(Mathf.Sin(bobTimer)) * vertical;

        cam.transform.localPosition = defaultCamLocalPos + new Vector3(x, y, 0f);
    }

    private void ResetCameraPosition()
    {
        bobTimer = 0f;
        cam.transform.localPosition = Vector3.Lerp(
            cam.transform.localPosition,
            defaultCamLocalPos,
            Time.deltaTime * 8f
        );
    }

    private void HandleZoom()
    {
        float targetFOV = motor.isAiming ? aimFOV : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * aimLerpSpeed);
    }

    private void UpdateCrosshairSpread()
    {
        if (!motor) return;

        float targetSpread = motor.isAiming ? aimSpread : normalSpread;

        currentCrosshairSpread = Mathf.Lerp(
            currentCrosshairSpread,
            targetSpread,
            Time.deltaTime * crosshairLerpSpeed
        );

        UpdateCrosshairPositions();
    }

    private void UpdateCrosshairPositions()
    {
        if (crosshairTop != null)
        {
            Vector2 newPos = topStartPos + new Vector2(0, currentCrosshairSpread);
            crosshairTop.anchoredPosition = Vector2.Lerp(
                crosshairTop.anchoredPosition,
                newPos,
                Time.deltaTime * crosshairLerpSpeed
            );
        }

        if (crosshairBottom != null)
        {
            Vector2 newPos = bottomStartPos + new Vector2(0, -currentCrosshairSpread);
            crosshairBottom.anchoredPosition = Vector2.Lerp(
                crosshairBottom.anchoredPosition,
                newPos,
                Time.deltaTime * crosshairLerpSpeed
            );
        }

        if (crosshairLeft != null)
        {
            Vector2 newPos = leftStartPos + new Vector2(-currentCrosshairSpread, 0);
            crosshairLeft.anchoredPosition = Vector2.Lerp(
                crosshairLeft.anchoredPosition,
                newPos,
                Time.deltaTime * crosshairLerpSpeed
            );
        }

        if (crosshairRight != null)
        {
            Vector2 newPos = rightStartPos + new Vector2(currentCrosshairSpread, 0);
            crosshairRight.anchoredPosition = Vector2.Lerp(
                crosshairRight.anchoredPosition,
                newPos,
                Time.deltaTime * crosshairLerpSpeed
            );
        }
    }
}