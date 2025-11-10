// Fuentes: 
using Unity.Netcode;

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    
    // Solo el servidor modifica el marcador para evitar trampas

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int delta)
    {
        Score.Value += delta;
    }
}