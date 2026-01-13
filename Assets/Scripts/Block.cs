using UnityEngine;

public class Block : MonoBehaviour
{
    public int blockID;
    public float health = 1.0f; // Blokların canı (Örn: 1 saniyede kırılsın)

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            // Bu kısım boş kalabilir, PlayerInteraction içinde yok edeceğiz
        }
    }
}