using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.AI;
using System.Net;
using Unity.VisualScripting;
using TMPro;

public class NetworkLobbyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyPanel; // Panel for lobby UI
    [SerializeField] private TMP_InputField ipInput; // "127.0.0.1" o IP LAN del host
    [SerializeField] private TMP_InputField portInput; // "7777" puerto del host
    [SerializeField] private Button hostButton; // Botón para iniciar como host
    [SerializeField] private Button clientButton; // Botón para iniciar como cliente
    [SerializeField] private Button shutdownButton; // Botón para detener la conexión
    [SerializeField] TMP_Text statusText; // Texto para mostrar el estado de la conexión

    [Header("Juego")]
    [SerializeField] private string gameSceneName = "GameScene"; // Nombre de la escena del juego
    [SerializeField] private int minPlayersToStart = 2; // Número mínimo de jugadores para iniciar el juego

    private NetworkManager nm;
    private UnityTransport transport;

    private void Awake()
    {
        nm = NetworkManager.Singleton;
        if (!nm)
        {
            Debug.LogError("falta NetworkManager en la escena.");
            enabled = false;
            return;
        }

        transport = nm.GetComponent<UnityTransport>();
        if (!transport)
        {
            Debug.LogError("falta UnityTransport en el NetworkManager.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        hostButton.onClick.AddListener(OnClickHost);
        clientButton.onClick.AddListener(OnClickClient);
        shutdownButton.onClick.AddListener(OnClickShutdown);

        nm.OnClientConnectedCallback += HandleClientConnected;
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
            nm.OnClientConnectedCallback -= HandleClientConnected;
            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnClickHost()
    {
        if (!TryGetAddressAndPort(out string _, out ushort port)) return;

        // Elhost escucha en todas las interfaces
        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");

        if (nm.StartHost())
        {
            if (statusText) statusText.text = $"Host escuchando en el puerto {port}. Esperando jugadores...";
            SetLobbInteractable(false);
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
            if (statusText) statusText.text = $"Conectando al servidor en {address}:{port}...";
            SetLobbInteractable(false);
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
        SetLobbInteractable(true);
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

    private void HandleClientConnected(ulong clientId)
    {
        if (nm.IsServer)
        {
            int count = nm.ConnectedClients.Count;
            if (statusText) statusText.text = $"Cliente {clientId} conectado. Jugadores: {count}/{minPlayersToStart}.";

            if (count >= minPlayersToStart)
            {
                nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                if (lobbyPanel) lobbyPanel.SetActive(false);
            }
        }
        else
        {
            if (statusText) statusText.text = $"Conectando. Cliente ID: {nm.LocalClientId}.";
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (statusText) statusText.text = $"Cliente {clientId} desconectado.";
    }

    private void SetLobbInteractable(bool enabled)
    {
        if (hostButton) hostButton.interactable = enabled;
        if (clientButton) clientButton.interactable = enabled;
        if (ipInput) ipInput.interactable = enabled;
        if (portInput) portInput.interactable = enabled;
    }

}
