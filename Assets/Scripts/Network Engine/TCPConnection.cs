using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// A stable connection which is guaranteed to have successful communication, with a latency tradeoff.
	/// </summary>
	public class TCPConnection
	{
		private bool IsClient => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsClient;
		private bool IsServer => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsServer;
		public int PlayerID { get; private set; }
		public Socket Socket { get; private set; }
		public bool IsDisconnecting { get; set; } = false;
		public bool HasDisconnected { get; set; } = false;

		private StringBuilder stringBuilder = new StringBuilder();
		private const int BufferSize = 4096;
		private byte[] buffer = new byte[BufferSize];
		private bool messageReady = false;

		public TCPConnection(int playerID, Socket socket)
		{
			PlayerID = playerID;
			Socket = socket;
		}

		/// <summary>
		/// Receive functionality for the TCP connection.
		/// </summary>
		public IEnumerator Receive()
		{
			while (Socket.Connected)
			{
				Socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(OnTCPReceive), this);

				// Wait until a message has been received.
				yield return new WaitUntil(() => messageReady || !Socket.Connected);

				// Pull message
				string response = stringBuilder.ToString();
				stringBuilder.Clear();
				messageReady = false;

				if (response.Length > 0 && response[response.Length - 1] == '\n')
				{
					// Parse string for commands
					string[] commands = response.Split('\n');
					for (int i = 0; i < commands.Length; i++)
					{
						HandleCommand(commands[i]);
					}
				}
			}
		}

		/// <summary>
		/// Callback for after a TCP message is received.
		/// </summary>
		private void OnTCPReceive(IAsyncResult result)
		{
			int bytesRead = Socket.EndReceive(result);
			if (bytesRead > 0)
			{
				stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
				messageReady = true;
			}
			else
			{
				Socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(OnTCPReceive), this);
			}
		}

		/// <summary>
		/// Handles individual commands from the TCP connection based on the message <see cref="NetworkMessage.Type"/>
		/// </summary>
		private void HandleCommand(string command)
		{
			NetworkMessage.Parameters param = NetworkMessage.GetParameters(command);
			if (param.type == NetworkMessage.Type.PlayerID)
			{
				if (IsClient && param.args.Count > 0 && int.TryParse(param.args[0], out int id))
				{
					PlayerID = id;
					NetworkCore.ActiveNetwork.LocalPlayerId = PlayerID;
				}
			}
			else if (param.type == NetworkMessage.Type.Disconnect)
			{
				if (IsServer && int.TryParse(param.args[0], out int disconnectID))
				{
					NetworkCore.ActiveNetwork.DisconnectUser(disconnectID);
				}
				else if (IsClient)
				{
					NetworkCore.ActiveNetwork.LeaveGame();
				}
			}
			else if (param.type == NetworkMessage.Type.Create)
			{
				if (IsClient && param.args.Count >= 3)
				{
					Vector3 position = Vector3.zero;
					if (param.args.Count >= 6)
					{
						try
						{
							position = new Vector3(float.Parse(param.args[3]), float.Parse(param.args[4]), float.Parse(param.args[5]));
						}
						catch
						{
							position = Vector3.zero;
						}
					}

					Quaternion rotation = Quaternion.identity;
					if (param.args.Count >= 10)
					{
						try
						{
							rotation = new Quaternion(float.Parse(param.args[6]), float.Parse(param.args[7]), float.Parse(param.args[8]), float.Parse(param.args[9]));
						}
						catch
						{
							rotation = Quaternion.identity;
						}
					}

					if (int.TryParse(param.args[0], out int contractID) && int.TryParse(param.args[1], out int owner) && int.TryParse(param.args[2], out int netID))
					{
						GameObject newGameObject = UnityEngine.Object.Instantiate(contractID == -1 ? NetworkItems.PlayerObject : NetworkItems.Lookup[contractID], position, rotation);

						if (newGameObject.TryGetComponent(out NetworkID networkID))
						{
							networkID.Owner = owner;
							networkID.NetId = netID;
							NetworkCore.ActiveNetwork.NetObjects[netID] = networkID;
						}
					}
				}
			}
			else if (param.type == NetworkMessage.Type.Delete)
			{
				if (NetworkCore.ActiveNetwork.IsClient && param.args.Count > 0 && int.TryParse(param.args[0], out int netID))
				{
					NetworkCore.ActiveNetwork.DestroyNetworkObject(netID);
				}
			}
			else if ((param.type == NetworkMessage.Type.Command && IsServer) || (param.type == NetworkMessage.Type.Update && IsClient))
			{
				if (param.args.Count >= 2 && int.TryParse(param.args[0], out int netID) && NetworkCore.ActiveNetwork.NetObjects.ContainsKey(netID))
				{
					string type = param.args[1];
					param.args.RemoveRange(0, 2);
					NetworkCore.ActiveNetwork.NetObjects[netID].NetworkMessage(type, param.args);
				}
			}
		}

		/// <summary>
		/// Send byte data over the network. Due to the nature of a TCP connection, this is guaranteed to reach the other end (at some point).
		/// </summary>
		public void Send(byte[] byteData)
		{
			Socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(OnTCPSend), Socket);
		}

		/// <summary>
		/// Callback for after a TCP message is sent.
		/// </summary>
		private void OnTCPSend(IAsyncResult result)
		{
			if (IsDisconnecting && NetworkCore.ActiveNetwork.IsClient)
			{
				HasDisconnected = true;
			}
		}
	}
}