using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// A scriptable object that contains references to all the networked game objects used in the project.
	/// In almost all cases you should look at <see cref="NetworkItems"/> instead.
	/// </summary>
	[CreateAssetMenu(fileName = "Network Contract", menuName = "Network Contract"), HideMonoScript]
	public class NetworkContract : ScriptableObject
	{
		[DisableInPlayMode, SerializeField, ValidateInput("playerObject"), Tooltip("The game object created when a player joins the server.")]
		private GameObject playerObject;
		[DisableInPlayMode, SerializeField, ValidateInput("CheckDuplicates", ContinuousValidationCheck = true), Tooltip("Game objects to be synchronized over the network."), PropertySpace]
		private Item[] contractItems = new Item[0];

		public GameObject PlayerObject => playerObject;
		public Item[] ContractItems => contractItems;

		/// <summary>
		/// Used in the inspector to verify that all contract items are correct.
		/// </summary>
		private bool CheckDuplicates(ref string errorMessage)
		{
			UpdateContractID();

			List<int> ids = new List<int>();
			foreach (Item item in contractItems)
			{
				if (item.value == null)
				{
					errorMessage = "An entry does not have a gameObject assigned!";
					return false;
				}
				else if (!item.value.TryGetComponent(out NetworkID networkID))
				{
					errorMessage = $"No NetworkID component found on {item.value.name}. Game objects without a network ID component can't be added to the Network Contract.";
					return false;
				}
				else if (ids.Contains(networkID.ContractID))
				{
					errorMessage = $"Duplicate id detected on {item.value.name}!";
					return false;
				}

				ids.Add(item.id);
			}

			return true;
		}

		/// <summary>
		/// Forces an update on the contract items, generating contract ids according to their <see cref="NetworkID"/> component.
		/// </summary>
		[HideInPlayMode, Button("Force Contract ID Update"), PropertySpace]
		public void UpdateContractID()
		{
			foreach (Item item in contractItems)
			{
				item.id = item.value == null || !item.value.TryGetComponent(out NetworkID networkID) ? int.MaxValue : networkID.ContractID;
			}
		}

		[Serializable]
		public class Item
		{
			[ReadOnly]
			public int id;
			public GameObject value;
		}
	}
}