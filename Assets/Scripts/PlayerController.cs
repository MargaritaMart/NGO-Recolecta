using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -9.81f;
    private CharacterController cc;
    
    private Vector3 velocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Solo el propietario procesa entrada y mueve; el NetworkTransform replica a los demás
        if (!IsOwner) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0, v).normalized;
        Vector3 move = input * moveSpeed;

        // Convertir a espacio mundo en caso de usar camara distinta
        // move = transform.TransformDirection(move);

        // Gravedad Simple
        if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        // Aplicar movimiento
        Vector3 total = new Vector3(move.x, 0, move.z) + new Vector3(0, velocity.y, 0);
        cc.Move(total * Time.deltaTime);

        // Orientación opcional hacia la dirección de movimiento
        if (input.sqrMagnitude > 0.001f)
        {
            transform.forward = new Vector3(input.x, 0, input.z);
        }
    }
}