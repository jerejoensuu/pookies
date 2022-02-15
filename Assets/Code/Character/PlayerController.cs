using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    
    GameObject player;
    Collider col;
    GameObject camTarget;
    Inputs inputs;
    CharacterController controller;

    public float gravity = 10;
    public float speed = 5;
    public float jumpSpeed = 2;
    public float accelSpeed = 0.0025f;
    public float decelSpeed = 0.005f;

    Vector3 movementDirection;
    Vector2 directionInput;
    Vector2 trueDirection;
    private float accel = 0;
    private bool turningCamera, moving, jumping;
    private Vector2 cameraRotation;
    public float stickSensitivity = 2;
    public float mouseSensitivity = 2;
    public float autoRotateSpeed;
    public float cameraHeight;

    RaycastHit rayHit;
    bool grounded;
    Vector3 center;
    Vector3 size;

    void Start() {
        player = transform.Find("Model").gameObject;
        col = player.GetComponent<Collider>();
        camTarget = transform.Find("Camera Target").gameObject;
        controller = GetComponent<CharacterController>();

        inputs = new Inputs();
        inputs.Enable();
        inputs.Player.LookStick.performed += ReadCameraInputStick;
        inputs.Player.LookStick.canceled += ReadCameraInputStick;
        inputs.Player.LookMouse.performed += ReadCameraInputMouse;
        inputs.Player.LookMouse.canceled += ReadCameraInputMouse;
        inputs.Player.CenterCamera.performed += CenterCamera;
        inputs.Player.Move.performed += ReadMovement;
        inputs.Player.Move.canceled += ReadMovement;
        inputs.Player.Jump.performed += Jump;
        inputs.Player.Jump.canceled += Jump;
    }

    void Update() {
        RotateCamera();
        AutoRotateCamera();
        ApplyMovement();
        GroundCheck();
    }

    void ApplyMovement() {
        // dampen movement:
        if (moving) {
            accel += accelSpeed;
            if (accel >= 1) accel = 1;
            RotatePlayer();
        } else {
            accel -= decelSpeed;
            if (accel <= 0) accel = 0;
        }
        // change player velocity based on input and camera rotation:
        movementDirection.x = directionInput.x * accel;
        movementDirection.z = directionInput.y * accel;
        if (grounded && movementDirection.y < 0) {
            movementDirection.y = 0;
        } else {
            movementDirection.y -= gravity * Time.deltaTime;
        }
        controller.Move(Quaternion.AngleAxis(camTarget.transform.eulerAngles.y, Vector3.up) * movementDirection * Time.deltaTime);
        camTarget.transform.position = player.transform.position + (Vector3.up * cameraHeight); // move camera to player:
    }

    Vector3 GetTrueDirection() {
        return Quaternion.AngleAxis(camTarget.transform.eulerAngles.y, Vector3.up) * new Vector3(directionInput.x, 0, directionInput.y);
    }

    void RotatePlayer() {
        player.transform.forward = Vector3.Slerp(player.transform.forward, GetTrueDirection(), 20 * Time.deltaTime);
    }

    void ReadMovement(InputAction.CallbackContext context) {
        moving = context.performed;
        if (context.performed) directionInput = context.ReadValue<Vector2>() * speed;
    }

    void Jump(InputAction.CallbackContext context) {
        if (!grounded || jumping) return;
        jumping = context.performed;
        movementDirection.y = jumpSpeed;
    }

    void ReadCameraInputStick(InputAction.CallbackContext context) {
        turningCamera = context.performed;
        cameraRotation.x = stickSensitivity * context.ReadValue<Vector2>().x;
    }

    void ReadCameraInputMouse(InputAction.CallbackContext context) {
        turningCamera = context.performed;
        cameraRotation.x = mouseSensitivity * context.ReadValue<Vector2>().x;
    }

    void RotateCamera() {
        if (!turningCamera) return;
        
        camTarget.transform.eulerAngles += new Vector3(0, cameraRotation.x, 0) * Time.deltaTime;
    }

    void AutoRotateCamera() {
        if (turningCamera || !moving) return;
        float rotateFactor = Vector3.Dot(camTarget.transform.right, Vector3.Normalize(GetTrueDirection()));
        camTarget.transform.eulerAngles += new Vector3(0, rotateFactor * autoRotateSpeed, 0) * Time.deltaTime;
    }

    void CenterCamera(InputAction.CallbackContext context) {
        StartCoroutine(PointCameraAt(GetTrueDirection()));
    }

    IEnumerator PointCameraAt(Vector3 direction) {
        while(Mathf.Abs(Vector3.SignedAngle(direction, camTarget.transform.forward, Vector3.up)) > 3) {
            if (turningCamera) break;
            camTarget.transform.forward = Vector3.Slerp(camTarget.transform.forward, direction, 25 * Time.deltaTime);
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    void GroundCheck() {
        center = new Vector3(player.transform.position.x, col.bounds.min.y + 0.1f, player.transform.position.z);
        size = new Vector3(player.transform.localScale.x, 0, player.transform.localScale.z) * 0.9f;
        grounded = Physics.BoxCast(center, size/2, Vector3.down, out rayHit, player.transform.rotation, 0.2f);
        if (grounded) {
            jumping = false;
        }
        Debug.Log(rayHit.collider);
    }

    void OnDrawGizmos() {
        if (grounded) {
            Gizmos.color = Color.green;
        } else {
            Gizmos.color = Color.red;
        }

        //Draw a Ray forward from GameObject toward the hit
        Gizmos.DrawRay(center, Vector3.down * rayHit.distance);
        //Draw a cube that extends to where the hit exists
        Gizmos.DrawWireCube(center + Vector3.down * (rayHit.distance / 2), new Vector3(size.x, rayHit.distance, size.z));
    }

    void OnDisable() {
        inputs.Disable();
        inputs.Player.LookStick.performed -= ReadCameraInputStick;
        inputs.Player.LookStick.canceled -= ReadCameraInputStick;
        inputs.Player.LookMouse.performed -= ReadCameraInputMouse;
        inputs.Player.LookMouse.canceled -= ReadCameraInputMouse;
        inputs.Player.CenterCamera.performed -= CenterCamera;
        inputs.Player.Move.performed -= ReadMovement;
        inputs.Player.Move.canceled -= ReadMovement;
        inputs.Player.Jump.performed -= Jump;
        inputs.Player.Jump.canceled -= Jump;
    }
}
