using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Responsible for managing low level functionality of the network.
	/// </summary>
	/// <remarks>When connected use <see cref="ActiveNetwork"/> to act upon the network.</remarks>
	public sealed class NetworkCore : MonoBehaviour
	{
		public static NetworkCore ActiveNetwork { get; private set; }

		[SerializeField, DisableInPlayMode, Range(1, 128), Title("Network Settings")]
		private int maxConnections = 32;
		[SerializeField, DisableInPlayMode, Tooltip("Objects created, with server ownership, when the server is started.")]
		private int[] initialObjects = new int[0];
		public const int UpdateRate = 50;
		public const float UpdateDelta = 1f / UpdateRate;

		public Dictionary<int, TCPConnection> Connections { get; private set; } = new Dictionary<int, TCPConnection>();
		public Dictionary<int, NetworkID> NetObjects { get; private set; } = new Dictionary<int, NetworkID>();
		public event Action<int> ClientConnected = delegate { };
		public event Action<int> ClientDisconnected = delegate { };
		public event Action NetworkTick = delegate { };

		#region Variables
		private Coroutine serverListener;
		private Socket listenerTCP;

		// Network State
		public int LocalPlayerId { get; set; } = -1;
		public bool IsServer { get; private set; } = false;
		public bool IsClient { get; private set; } = false;
		private bool CurrentlyConnecting { get; set; } = false;
		private bool IsConnected { get; set; } = false;
		public int ConnectionCount { get; set; } = 0;
		public int ObjectCount { get; set; } = 0;

		// Message
		public bool MessageWaiting { get; set; } = false;
		public string MasterMessage { get; set; } = string.Empty;

		// Network configuration
		public string IpAddress { get; private set; } = "127.0.0.1";
		public int Port { get; private set; } = 9001;
		#endregion

		private void Awake()
		{
			ActiveNetwork = this;
		}

		public void OnApplicationQuit()
		{
			LeaveGame();
		}

		/// <summary>
		/// Leave the game by disconnecting or closing the server.
		/// </summary>
		public void LeaveGame()
		{
			if (IsClient && IsConnected && !Connections[0].IsDisconnecting)
			{
				StartCoroutine(LeaveGameClient());
			}

			if (IsServer && IsConnected)
			{
				LeaveGameServer();
			}
		}

		/// <summary>
		/// Destroy an object on the network.
		/// </summary>
		/// <param name="netID">The ID of the object to destroy.</param>
		/// <remarks>
		/// Can work on server and client, however only the server can propogate the message to clients. 
		/// Therefore, on clients this should only be used as a response to a server command.
		/// </remarks>
		public void DestroyNetworkObject(int netID)
		{
			if (NetObjects.ContainsKey(netID))
			{
				Destroy(NetObjects[netID].gameObject);
				NetObjects.Remove(netID);
			}

			if (IsServer)
			{
				MasterMessage += NetworkMessage.CreateMessage(NetworkMessage.Type.Delete, netID.ToString());
			}
		}

		/// <summary>
		/// Send messages to active connections when a message is ready.
		/// </summary>
		private IEnumerator NetworkUpdate()
		{
			float updateTimer = 0;
			while (IsConnected)
			{
				//Compose Master Message
				foreach (NetworkID id in NetObjects.Values)
				{
					MasterMessage += id.EnqueuedMessage;
					id.ClearMessage();
				}

				//Send Master Message
				List<int> invalidConnections = new List<int>();
				if (MasterMessage != string.Empty)
				{
					byte[] byteData = Encoding.ASCII.GetBytes(MasterMessage);
					foreach (KeyValuePair<int, TCPConnection> connection in Connections)
					{
						try
						{
							connection.Value.Send(byteData);
						}
						catch
						{
							invalidConnections.Add(connection.Key);
						}
					}

					MasterMessage = string.Empty;
					MessageWaiting = false;

					foreach (int invalidConnection in invalidConnections)
					{
						DisconnectUser(invalidConnection);
					}
				}

				do
				{
					updateTimer += UpdateDelta;

					while (updateTimer > 0)
					{
						updateTimer = Mathf.MoveTowards(updateTimer, UpdateDelta * -5, Time.deltaTime);
						yield return null;
					}

					NetworkTick();
				}
				while (!MessageWaiting && MasterMessage == string.Empty);
			}
		}

		#region Client Functions
		/// <summary>
		/// Start a client instance, if not currently connected.
		/// </summary>
		public void StartClient()
		{
			if (!IsConnected && !CurrentlyConnecting)
			{
				StartCoroutine(ConnectClient());
			}
		}

		/// <summary>
		/// Handles the connection of Client to the Server.
		/// </summary>
		private IEnumerator ConnectClient()
		{
			IsServer = false;
			IsClient = false;
			IsConnected = false;
			CurrentlyConnecting = true;

			//Setup our socket
			IPAddress ip = IPAddress.Parse(IpAddress);
			IPEndPoint endPoint = new IPEndPoint(ip, Port);
			Socket clientSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			//Connect client
			clientSocket.BeginConnect(endPoint, ConnectingCallback, clientSocket);

			//Wait for the client to connect
			yield return new WaitUntil(() => IsConnected);
			CurrentlyConnecting = false;
			StartCoroutine(Connections[0].Receive());  //It is 0 on the client because we only have 1 socket.
			StartCoroutine(NetworkUpdate());  //This will allow the client to send messages to the server.
		}

		/// <summary>
		/// Fired when the client has successfully connected to the server.
		/// </summary>
		private void ConnectingCallback(IAsyncResult result)
		{
			TCPConnection newConnection = new TCPConnection(-1, (Socket)result.AsyncState);
			newConnection.Socket.EndConnect(result);
			Connections.Add(0, newConnection);

			IsClient = true;
			IsConnected = true;
		}

		/// <summary>
		/// Disconnects the client from the server.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private IEnumerator LeaveGameClient()
		{
			if (IsClient)
			{
				Connections[0].IsDisconnecting = true;
				string disconnectMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.Disconnect, Connections[0].PlayerID.ToString());
				Connections[0].Send(Encoding.ASCII.GetBytes(disconnectMessage));
				yield return new WaitUntil(() => Connections[0].HasDisconnected);
				DisconnectClient();
			}
		}

		/// <summary>
		/// Forcibly disconnects a client from the server.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private void DisconnectClient()
		{
			if (IsClient)
			{
				if (Connections.TryGetValue(0, out TCPConnection connection) && connection.Socket.Connected)
				{
					connection.Socket.Shutdown(SocketShutdown.Both);
					connection.Socket.Close();
					Connections.Remove(0);
				}

				IsClient = false;
				IsServer = false;
				IsConnected = false;
				LocalPlayerId = -10;

				foreach (NetworkID id in NetObjects.Values)
				{
					Destroy(id.gameObject);
				}

				NetObjects.Clear();
				Connections.Clear();
			}
		}
		#endregion

		#region Server Functions
		/// <summary>
		/// Starts the server if not currently running as a client or server already.
		/// </summary>
		public void StartServer()
		{
			if (!IsConnected)
			{
				serverListener = StartCoroutine(ServerListen());
				StartCoroutine(NetworkUpdate());
			}
		}

		/// <summary>
		/// Runs on the server to allow clients to connect and handle behavior when they do connect.
		/// </summary>
		private IEnumerator ServerListen()
		{
			//If we are listening then we are the server.
			IsServer = true;
			IsClient = false;
			IsConnected = true;
			LocalPlayerId = -1;
			CurrentlyConnecting = false;

			IPAddress ip = IPAddress.Any;
			IPEndPoint endPoint = new IPEndPoint(ip, Port);

			//We could do UDP in some cases but for now we will do TCP
			listenerTCP = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			//Now I have a socket listener.
			listenerTCP.Bind(endPoint);
			listenerTCP.Listen(maxConnections);

			foreach (int initialObject in initialObjects)
			{
				CreateNetworkObject(initialObject);
			}

			while (IsConnected)
			{
				listenerTCP.BeginAccept(new AsyncCallback(ServerListenCallback), listenerTCP);
				yield return new WaitUntil(() => CurrentlyConnecting);
				CurrentlyConnecting = false;

				if (Connections.ContainsKey(ConnectionCount - 1))
				{
					string playerIDMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.PlayerID, Connections[ConnectionCount - 1].PlayerID.ToString());
					Connections[ConnectionCount - 1].Send(Encoding.ASCII.GetBytes(playerIDMessage));

					//Start Server side listening for client messages.
					StartCoroutine(Connections[ConnectionCount - 1].Receive());

					//Update all current network objects
					foreach (KeyValuePair<int, NetworkID> entry in NetObjects)
					{
						List<string> args = new List<string>()
						{
							entry.Value.ContractID.ToString(),
							entry.Value.Owner.ToString(),
							entry.Value.NetId.ToString(),
							entry.Value.transform.position.x.ToString("N2"),
							entry.Value.transform.position.y.ToString("N2"),
							entry.Value.transform.position.z.ToString("N2"),
							entry.Value.transform.rotation.x.ToString("N2"),
							entry.Value.transform.rotation.y.ToString("N2"),
							entry.Value.transform.rotation.z.ToString("N2"),
							entry.Value.transform.rotation.w.ToString("N2")
						};
						string message = NetworkMessage.CreateMessage(NetworkMessage.Type.Create, args);
						Connections[ConnectionCount - 1].Send(Encoding.ASCII.GetBytes(message));
					}

					//Create NetworkPlayerManager
					CreateNetworkObject(-1, ConnectionCount - 1, Vector3.zero);

					ClientConnected(ConnectionCount - 1);
				}
			}
		}

		/// <summary>
		/// Occurs when a client connects to the server.
		/// </summary>
		private void ServerListenCallback(IAsyncResult result)
		{
			Socket listener = (Socket)result.AsyncState;
			Socket handler = listener.EndAccept(result);
			TCPConnection newConnection = new TCPConnection(ConnectionCount, handler);
			ConnectionCount++;
			Connections.Add(newConnection.PlayerID, newConnection);
			CurrentlyConnecting = true;
		}

		/// <summary>
		/// Removes a connection from the server
		/// </summary>
		public void DisconnectUser(int connectionID)
		{
			if (IsServer && Connections.TryGetValue(connectionID, out TCPConnection connection))
			{
				try
				{
					connection.Socket.Shutdown(SocketShutdown.Both);
					connection.Socket.Close();
				}
				catch { }

				DestroyUserObjects(connectionID);
				ClientDisconnected(connectionID);
				Connections.Remove(connectionID);
			}
		}

		/// <summary>
		/// Destroy all network objects owned by a particular user.
		/// </summary>
		/// <param name="ownerID">The user ID of the client to destroy objects belonging to.</param>
		private void DestroyUserObjects(int ownerID)
		{
			if (IsServer)
			{
				List<int> invalidObjects = new List<int>();
				foreach (KeyValuePair<int, NetworkID> netObject in NetObjects)
				{
					if (netObject.Value.Owner == ownerID)
					{
						invalidObjects.Add(netObject.Key);
					}
				}

				for (int i = 0; i < invalidObjects.Count; i++)
				{
					DestroyNetworkObject(invalidObjects[i]);
				}
			}
		}

		/// <summary>
		/// Instantiate an object over the network.
		/// </summary>
		/// <param name="contractID">The ID of the object inside <see cref="NetworkItems"/></param>
		/// <param name="owner">The owner of the object on the network.</param>
		/// <returns>The <see cref="GameObject"/> instantiated when <see cref="IsServer"/> is true. Otherwise returns null.</returns>
		/// <remarks>Can only be called by the server (determined by <see cref="IsServer"/>)</remarks>
		public GameObject CreateNetworkObject(int contractID, int owner = -1, Vector3 position = default, Quaternion rotation = default)
		{
			if (IsServer)
			{
				GameObject newNetworkObject = Instantiate(contractID != -1 ? NetworkItems.Lookup[contractID] : NetworkItems.PlayerObject, position, rotation);
				if (newNetworkObject.TryGetComponent(out NetworkID networkID))
				{
					networkID.Owner = owner;
					networkID.NetId = ObjectCount;
					NetObjects[ObjectCount] = networkID;
					ObjectCount++;

					List<string> args = new List<string>()
					{
						contractID.ToString(),
						owner.ToString(),
						(ObjectCount - 1).ToString(),
						position.x.ToString("N2"),
						position.y.ToString("N2"),
						position.z.ToString("N2"),
						rotation.x.ToString("N2"),
						rotation.y.ToString("N2"),
						rotation.z.ToString("N2"),
						rotation.w.ToString("N2"),
					};
					MasterMessage += NetworkMessage.CreateMessage(NetworkMessage.Type.Create, args);
				}
				return newNetworkObject;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Stops running the server and disconnects all clients.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private void LeaveGameServer()
		{
			if (IsServer && IsConnected)
			{
				List<int> disconnectTargets = new List<int>();
				foreach (int userID in Connections.Keys)
				{
					string disconnectMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.Disconnect, "-1");
					Connections[userID].Send(Encoding.ASCII.GetBytes(disconnectMessage));
					Connections[userID].IsDisconnecting = true;
					disconnectTargets.Add(userID);
				}

				foreach (int disconnectTarget in disconnectTargets)
				{
					DisconnectUser(disconnectTarget);
				}

				foreach (NetworkID obj in NetObjects.Values)
				{
					if (obj != null)
					{
						Destroy(obj.gameObject);
					}
				}

				IsConnected = false;
				IsServer = false;
				IsClient = false;
				CurrentlyConnecting = false;

				try
				{
					listenerTCP.Close();
				}
				catch { }

				NetObjects.Clear();
				Connections.Clear();
			}
		}
		#endregion

		#region UI Methods
		public void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
			Application.Quit();
#endif
		}

		public void StopListening()
		{
			if (IsServer && CurrentlyConnecting)
			{
				CurrentlyConnecting = false;
				StopCoroutine(serverListener);
				listenerTCP.Close();
			}
		}
		#endregion

		#region OnQuit Handling
		private void OnEnable()
		{
			Application.wantsToQuit += Application_wantsToQuit;
		}

		private void OnDisable()
		{
			Application.wantsToQuit -= Application_wantsToQuit;
		}

		private bool Application_wantsToQuit()
		{
			if (IsConnected || IsServer)
			{
				LeaveGame();
				return false;
			}
			else
			{
				return true;
			}
		}
		#endregion
	}
}
