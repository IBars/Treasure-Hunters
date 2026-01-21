using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Kamera ve Bakış Ayarları")]
    public Transform playerCamera;
    public float mouseSensitivity = 200f;
    private float xRotation = 0f;

    [Header("Hareket Ayarları")]
    public float walkSpeed = 4.3f;
    public float sprintSpeed = 5.6f;
    public float jumpForce = 1.25f;
    public float gravity = -28f;

    [Header("Su Ayarları")]
    public float waterSpeed = 2.5f;      // Su altı hızı
    public float waterGravity = -5f;    // Su altı yer çekimi (hafif batış)
    public float swimUpForce = 4f;      // Boşlukla yükselme gücü
    private bool isInWater = false;     // Su içinde miyiz?

    [Header("Fizik Ayarları")]
    public float stepHeight = 0.6f;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepHeight;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook(); // Kamera her zaman çalışır, bozulmaz.
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 0.02f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
{
    // EKLENEN KONTROL: Controller kapalıysa fonksiyondan çık
    if (controller == null || !controller.enabled) return;

    isGrounded = controller.isGrounded;
    // ... geri kalan hareket kodların aynı kalabilir ...

        isGrounded = controller.isGrounded;

        // Yere değiyorsak velocity'yi sıfırla (Su dışındayken)
        if (isGrounded && velocity.y < 0 && !isInWater)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 1. Yatay Hareket Hesaplama
        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        float currentSpeed;

        if (isInWater)
        {
            currentSpeed = waterSpeed; // Su altı hızı sabit
        }
        else
        {
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0;
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        // 2. Dikey Hareket (Zıplama / Yüzme)
        if (isInWater)
        {
            // SU İÇİNDE FİZİK
            if (Input.GetButton("Jump")) // Space basılı tutulursa yüksel
            {
                velocity.y = swimUpForce;
            }
            else
            {
                // Yavaşça batış efekti
                velocity.y += waterGravity * Time.deltaTime;
                if (velocity.y < -1.5f) velocity.y = -1.5f; // Çok hızlı batmayı engelle
            }
        }
        else
        {
            // KARADA FİZİK
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            velocity.y += gravity * Time.deltaTime;
        }

        // Nihai dikey hareket
        controller.Move(velocity * Time.deltaTime);
    }

    // Suya giriş çıkış kontrolü (Water prefabında "Water" Tag'i ve "Is Trigger" açık olmalı)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            velocity.y /= 2f; // Suya girince hızı biraz kır (yumuşak giriş)
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = false;
        }
    }
}