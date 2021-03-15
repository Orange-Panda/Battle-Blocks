using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	[SerializeField]
	private Renderer[] renderers;

	public override void HandleMessage(string command, List<string> args)
	{
		
	}

	protected override IEnumerator NetworkUpdate()
	{
		if (TryGetComponent(out Rigidbody rigidbody))
		{
			if (IsClient)
			{
				Destroy(rigidbody);
			}
			else if (IsServer)
			{
				Spawner spawner = FindObjectOfType<Spawner>();
				rigidbody.position = spawner.playerPoints[Random.Range(0, spawner.playerPoints.Length)];
			}
		}

		yield return null;

		foreach (Renderer renderer in renderers)
		{
			if (IsClient && IsLocalPlayer)
			{
				renderer.material.color = Color.green; 
			}
			else if (IsClient && !IsLocalPlayer)
			{
				renderer.material.color = Color.red;
			}
		}
	}
}