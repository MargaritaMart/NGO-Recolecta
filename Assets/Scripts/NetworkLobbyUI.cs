using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyPanel;   // Panel del lobby
    [SerializeField] private TMP_InputField ipInput;        
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button shutdownButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Juego")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private int minPlayersToStart = 2; // Host + 1 cliente


    private NetworkManager nm;
    private UnityTransport transport;


    private void Awake()
    {
        nm = NetworkManager.Singleton;
        if (!nm)
        {
            Debug.LogError("Falta networkManager en la escena.");
            enabled = false;
            return;
        }
        transport = nm.GetComponent<UnityTransport>();
        if (!transport)
        {
            Debug.LogError("Falta UnityTransport en el NetworkManager.");
            enabled = false;
            return;
        }
    }

    private void Onable()
    {
        hostButton.onClick.AddListener(OnClickHost);
        clientButton.onClick.AddListener(OnClickClient);
        shutdownButton.onClick.AddListener(OnClickShutdown);

        nm.OnClientConnectedCallback += HandLeClientConnected;
        nm.OnClientDisconnectCallback += HandleClientDisconnected;

        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (shutdownButton) shutdownButton.gameObject.SetActive(false);
        if (statusText) statusText.text = "Listo para conectar.";
    }

    private void OnDisable()
    {
        hostButton.onClick.RemoveListener(OnClickHost);
        clientButton.onClick.RemoveListener(OnClickClient);
        shutdownButton.onClick.RemoveListener(OnClickShutdown);

        if (nm != null)
        {
            nm.OnClientConnectedCallback -= HandLeClientConnected;
            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnClickHost()
    {
        if (!TryGetAddressAndPort(out string _, out ushort port)) return;

        //el host escucha en todas las interfaces
        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");

        if (nm.StartHost())
        {
            if (statusText) statusText.text = $"host escuchando en puerto {port}. esperando jugadores...";
            SetLobbyInteractable(false);
            shutdownButton.gameObject.SetActive(true);
        }
        else
        {
            if (statusText) statusText.text = "Error al iniciar el host.";
        }

    }

    private void OnClickClient()
    {
        if (!TryGetAddressAndPort(out string address, out ushort port)) return;

        transport.SetConnectionData(address, port);

        if (nm.StartClient())
        {
            if (statusText) statusText.text = $"CLIENTT conectando a {address}:{port}_";
            SetLobbyInteractable(false);
            shutdownButton.gameObject.SetActive(true);
        }
        else
        {
            if (statusText) statusText.text = "Error al iniciar el cliente.";
        }
    }

    private void OnClickShutdown()
    {
        nm.Shutdown();
        if (statusText) statusText.text = "Conexión finalizada.";
        SetLobbyInteractable(true);
        shutdownButton.gameObject.SetActive(false);
    }

    private bool TryGetAddressAndPort(out string address, out ushort port)
    {
        address = (ipInput && !string.IsNullOrWhiteSpace(ipInput.text)) ? ipInput.text.Trim() : "127.0.0.1";
        string portStr = (portInput && !string.IsNullOrWhiteSpace(portInput.text)) ? portInput.text.Trim() : "7777";

        if (!ushort.TryParse(portStr, out port))
        {
            if (statusText) statusText.text = "Puerto inválido. Usa 1-65535.";
            return false;
        }
        return true;

    }

    private void HandLeClientConnected(ulong clientId)
    {
        if (nm.IsServer)
        {
            int count = nm.ConnectedClientsIds.Count;

            if (statusText) statusText.text = $"Cliente {clientId} conectado. Jugadores: {count}/{minPlayersToStart}";

            if (count >= minPlayersToStart)
            {
                nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                if (lobbyPanel) lobbyPanel.SetActive(false);
            }
            else
            {
                if (statusText) statusText.text = $"Conectado. ClientID local: {nm.LocalClientId}";
            }
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (statusText) statusText.text = $"Cliente {clientId} desconectado.";
    }

    private void SetLobbyInteractable(bool interactable)
    {
        if (ipInput) ipInput.interactable = enabled;
        if (portInput) portInput.interactable = enabled;
        if (hostButton) hostButton.interactable = enabled;
        if (clientButton) clientButton.interactable = enabled;
    }
    
}
