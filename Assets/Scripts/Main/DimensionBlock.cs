using UnityEngine;
using System.Collections;

public class DimensionBlock : MonoBehaviour
{
    [Header("Boyut Ayarları")]
    public Vector3 insidePosition = new Vector3(1000, 100, 1000);

    private static Vector3 lastOutsidePosition;
    private static bool isInside;

    void Awake()
    {
        // Unity Play/Stop sonrası kilitlenmesin diye
        isInside = false;
    }

    public void Interact(Transform playerTransform)
    {
        if (isInside) return;

        Debug.Log("DIMENSION INTERACT!");

        lastOutsidePosition = playerTransform.position + Vector3.up * 0.5f;

        if (DimensionManager.Instance != null)
            DimensionManager.Instance.CreateEmptyRoom(insidePosition);

        CharacterController cc = playerTransform.GetComponent<CharacterController>();

        StartCoroutine(TeleportRoutine(
            playerTransform,
            cc,
            insidePosition + new Vector3(0, 1.5f, 0),
            true
        ));
    }

    void Update()
    {
        if (!isInside) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            CharacterController cc = player.GetComponent<CharacterController>();

            StartCoroutine(TeleportRoutine(
                player.transform,
                cc,
                lastOutsidePosition,
                false
            ));
        }
    }

    IEnumerator TeleportRoutine(
        Transform player,
        CharacterController cc,
        Vector3 targetPos,
        bool goingInside
    )
    {
        if (cc != null) cc.enabled = false;

        yield return null; // 1 frame bekle (çok kritik)

        player.position = targetPos;

        yield return new WaitForSeconds(0.05f);

        if (cc != null) cc.enabled = true;

        if (goingInside)
        {
            if (ChunkWorldGenerator.InstanceGameObject != null)
                ChunkWorldGenerator.InstanceGameObject.SetActive(false);

            Camera.main.farClipPlane = 50f;
            isInside = true;
        }
        else
        {
            if (ChunkWorldGenerator.InstanceGameObject != null)
                ChunkWorldGenerator.InstanceGameObject.SetActive(true);

            Camera.main.farClipPlane = 1000f;
            isInside = false;
        }
    }
}
