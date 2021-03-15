using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Used to get objects defined in the <see cref="NetworkContract"/> at runtime.
	/// </summary>
	public static class NetworkItems
	{
		/// <summary>
		/// A dictionary of network game objects with a key of integer ContractID and value of GameObject.
		/// </summary>
		public static Dictionary<int, GameObject> Lookup { get; private set; } = new Dictionary<int, GameObject>();

		/// <summary>
		/// The object to be spawned when a player connects to the server.
		/// </summary>
		public static GameObject PlayerObject { get; private set; }

		//Initializes the Dictionary before it is used. See static constructors for more information.
		static NetworkItems()
		{
			NetworkContract contract = Resources.Load<NetworkContract>("Network Contract");
			if (contract)
			{
				contract.UpdateContractID();
				PlayerObject = contract.PlayerObject;

				foreach (NetworkContract.Item item in contract.ContractItems)
				{
					Lookup.Add(item.id, item.value);
				}
			}
			else
			{
				throw new NullReferenceException("No Network Contract found. Please create a Network Contract in Resources folder with the exact name \"Network Contract\".");
			}
		}
	}
}