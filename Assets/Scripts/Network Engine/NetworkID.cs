using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Used to identify a specific object on the network.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="NetId"/> which is the integer identifier, while this is the Monobehavior component that references it.</remarks>
	public sealed class NetworkID : MonoBehaviour
	{
		[DisableInPlayMode, SerializeField, Min(0), Tooltip("The unique contract id for this gameObject. Allows all clients to create the same object on each instance.")]
		private int contractID;

		/// <summary>
		/// All NetworkComponents on this gameObject.
		/// </summary>
		/// <remarks>This is initialized on Start(), therefore adding new NetworkComponents must be done before Start occurs.</remarks>
		private NetworkComponent[] components;

		// Properties
		/// <summary>
		/// The identifier for this particular instance of an object over the network.
		/// </summary>
		public int NetId { get; set; } = -10;
		/// <summary>
		/// The network user responsible for controlling this gameObject.
		/// </summary>
		public int Owner { get; set; } = -10;
		public bool NetworkReady { get; private set; }
		public string EnqueuedMessage { get; private set; } = string.Empty;

		// Get properties
		/// <summary>
		/// The unique contract id for this gameObject. Allows all clients to create the same object on each instance.
		/// </summary>
		public int ContractID => contractID;
		public bool IsServer => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsServer;
		public bool IsClient => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsClient;
		public bool IsLocalPlayer => NetworkReady && NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.LocalPlayerId == Owner;

		private IEnumerator Start()
		{
			yield return new WaitUntil(() => IsServer || IsClient);

			if (IsClient && NetId == -10)
			{
				Destroy(gameObject);
			}
			else if (IsServer && NetId == -10)
			{
				if (NetworkItems.Lookup.ContainsKey(contractID))
				{
					Owner = -1;
					NetId = NetworkCore.ActiveNetwork.ObjectCount;
					NetworkCore.ActiveNetwork.ObjectCount++;
					NetworkCore.ActiveNetwork.NetObjects.Add(NetId, this);
				}
				else
				{
					throw new System.Exception("FATAL - Game Object not found in network contract!");
				}
			}

			yield return new WaitUntil(() => Owner != -10 && NetId != -10);

			components = GetComponents<NetworkComponent>();
			NetworkReady = true;
		}

		public void AddMessage(string msg)
		{
			EnqueuedMessage += msg;
			NetworkCore.ActiveNetwork.MessageWaiting = true;
		}

		public void ClearMessage()
		{
			EnqueuedMessage = string.Empty;
		}

		public void NetworkMessage(string command, List<string> args)
		{
			if (NetworkReady)
			{
				if (IsServer && NetworkCore.ActiveNetwork.Connections.ContainsKey(Owner) == false && Owner != -1)
				{
					NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
				}
				else
				{
					foreach (NetworkComponent component in components)
					{
						component.HandleMessage(command, args);
					}
				}
			}
		}
	}
}