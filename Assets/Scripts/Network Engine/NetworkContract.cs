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
		[DisableInPlayMode, SerializeField, ValidateInput("VerifyIndex"), Tooltip("The game object created when a player joins the server.")]
		private int playerObjectIndex;
		[DisableInPlayMode, SerializeField, ValidateInput("CheckDuplicates", ContinuousValidationCheck = true), Tooltip("Game objects to be synchronized over the network."), PropertySpace]
		private Item[] contractItems = new Item[0];

		public GameObject PlayerObject => VerifyIndex() ? contractItems[playerObjectIndex].value : null;
		public Item[] ContractItems => contractItems;

		/// <summary>
		/// Used in the inspector to verify that all contract items are correct.
		/// </summary>
		private bool CheckDuplicates(ref string errorMessage, ref InfoMessageType messageType)
		{
			UpdateContractID();

			List<int> ids = new List<int>();
			foreach (Item item in contractItems)
			{
				if (item.value == null)
				{
					messageType = InfoMessageType.Error;
					errorMessage = "An entry does not have a gameObject assigned!";
					return false;
				}
				else if (!item.value.TryGetComponent(out NetworkID networkID))
				{
					messageType = InfoMessageType.Error;
					errorMessage = $"No NetworkID component found on {item.value.name}. Game objects without a network ID component can't be added to the Network Contract.";
					return false;
				}
				else if (ids.Contains(networkID.ContractID))
				{
					messageType = InfoMessageType.Error;
					errorMessage = $"Duplicate id detected on {item.value.name}!";
					return false;
				}

				ids.Add(item.id);
			}

			List<string> keys = new List<string>();
			bool validLookup = true;
			foreach (Item item in contractItems)
			{
				if (string.IsNullOrWhiteSpace(item.lookupKey))
				{
					messageType = InfoMessageType.Error;
					errorMessage += $"Empty lookup key detected on {item.value.name}\n";
					validLookup = false;
				}
				else if (keys.Contains(item.lookupKey))
				{
					messageType = InfoMessageType.Error;
					errorMessage += $"Duplicate lookup key detected on {item.value.name}\n";
					validLookup = false;
				}

				keys.Add(item.lookupKey);
			}

			return validLookup;
		}

		private bool VerifyIndex()
		{
			return playerObjectIndex >= 0 && playerObjectIndex < contractItems.Length;
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
			[Tooltip("A string lookup key that will try to find the id of a particular network item.")]
			public string lookupKey;
			public GameObject value;
		}
	}
}