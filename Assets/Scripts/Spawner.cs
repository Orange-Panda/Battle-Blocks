using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : NetworkComponent
{
	private List<GameObject> enemies = new List<GameObject>();
	public Vector3[] enemyPoints = new Vector3[4];
	public Vector3[] playerPoints = new Vector3[4];

	private void OnDrawGizmosSelected()
	{
		foreach (Vector3 spawnPoint in enemyPoints)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawCube(spawnPoint, Vector3.one);
		}

		foreach (Vector3 spawnPoint in playerPoints)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawCube(spawnPoint, Vector3.one);
			Gizmos.color = Color.white;
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		
	}

	protected override IEnumerator NetworkUpdate()
	{
		while (IsServer)
		{	
			if (NetworkCore.ActiveNetwork.Connections.Count > 0 && enemies.Count < 32 && NetworkItems.TryGetIndex("enemy", out int index))
			{
				enemies.Add(NetworkCore.ActiveNetwork.CreateNetworkObject(index, -1, enemyPoints[Random.Range(0, enemyPoints.Length)]));
				yield return new WaitForSeconds(0.1f);
			}

			yield return new WaitForSeconds(0.1f);

			for (int i = 0; i < enemies.Count; i++)
			{
				if (enemies[i] == null)
				{
					enemies.RemoveAt(i);
					break;
				}
			}
		}
	}
}