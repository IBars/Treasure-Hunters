using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Kamera ve Bakış Ayarları")]
    public Transform playerCamera;       // FPS Kamerasını buraya sürükle
    public float mouseSensitivity = 200f;
    private float xRotation = 0f;

    [Header("Hareket Ayarları")]
    public float walkSpeed = 4.3f;
    public float sprintSpeed = 5.6f;
    public float jumpForce = 1.25f;
    public float gravity = -28f;

    [Header("Fizik Ayarları")]
    public float stepHeight = 0.6f;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepHeight;

        // Mouse imlecini oyun ekranına kilitle ve gizle (MC gibi)
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook(); // Kamera kontrolü
        HandleMovement();  // Hareket kontrolü
    }

    void HandleMouseLook()
{
    // Mouse inputlarını ham (raw) olarak alıyoruz
    float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * 0.02f;
    float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 0.02f;

    // xRotation değerini güncelle
    xRotation -= mouseY;
    
    // xRotation'ı sınırlıyoruz (-90 ile 90 derece arası)
    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

    // KAMERA DÖNDÜRME: Yukarı-Aşağı (Sadece kamerayı döndürür)
    playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    // VÜCUT DÖNDÜRME: Sağa-Sola (Karakterin tamamını döndürür)
    transform.Rotate(Vector3.up * mouseX);
}

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}