using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameSpawner : NetworkBehaviour
{
    [Header("Spawn Bounds (centro en 0,0)")]
    [SerializeField] private float halfSize = 50f; // Terreno 100x100 => mitad 50

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject collectiblePrefab;

    [Header("Coleccionables")]
    [SerializeField] private int collectiblesCount = 30;

    // Un diccionario para rastrear qué NetworkObject pertenece a qué cliente
    private readonly Dictionary<ulong, NetworkObject> spawnedPlayers = new();

    // Nos suscribimos a los eventos del NetworkManager
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    // Se llama cuando el servidor (Host) termina de iniciarse
    private void OnServerStarted()
    {
        if (!IsServer) return;

        // Spawnea jugadores que ya estén conectados (ej. el Host)
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerFor(clientId);
        }

        // Spawnea los coleccionables una sola vez
        SpawnCollectibles();
    }

    // Se llama cada vez que un *nuevo* cliente se conecta
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnPlayerFor(clientId);
    }

    // Se llama cuando un cliente se desconecta
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        // Busca el objeto del jugador que se desconectó y lo elimina de la red
        if (spawnedPlayers.TryGetValue(clientId, out var netObj) && netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        spawnedPlayers.Remove(clientId);
    }

    // La lógica para crear un jugador
    private void SpawnPlayerFor(ulong clientId)
    {
        if (playerPrefab == null) { Debug.LogError("Falta playerPrefab en GameSpawner"); return; }

        Vector3 pos = GetRandomPointOnMap();
        var go = Instantiate(playerPrefab, pos, Quaternion.identity);
        var netObj = go.GetComponent<NetworkObject>();
        
        // Spawnea el objeto y le da "propiedad" (ownership) al cliente
        netObj.SpawnAsPlayerObject(clientId, true); // asigna ownership al cliente
        spawnedPlayers[clientId] = netObj;
    }

    // La lógica para crear los ítems coleccionables
    private void SpawnCollectibles()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogWarning("Falta collectiblePrefab, no se generarán coleccionables."); return;
        }

        for (int i = 0; i < collectiblesCount; i++)
        {
            Vector3 pos = GetRandomPointOnMap();
            var go = Instantiate(collectiblePrefab, pos, Quaternion.identity);
            var no = go.GetComponent<NetworkObject>();
            
            // Spawneo normal: el servidor es el dueño, no un cliente.
            no.Spawn(true);
        }
    }

    // Función de utilidad para encontrar un punto aleatorio en el mapa
    private Vector3 GetRandomPointOnMap()
    {
        float x = Random.Range(-halfSize + 1f, halfSize - 1f);
        float z = Random.Range(-halfSize + 1f, halfSize - 1f);
        return new Vector3(x, 0.05f, z); // un poco sobre el suelo
    }
}