using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : NetworkComponent
{
	public override void HandleMessage(string command, List<string> args)
	{
		
	}

	protected override IEnumerator NetworkUpdate()
	{
		while (IsServer)
		{
			Vector3 goal = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
			while (transform.position != goal)
			{
				transform.position = Vector3.MoveTowards(transform.position, goal, Time.deltaTime);
				yield return null;
			}
			yield return new WaitForSeconds(2f);
		}
	}
}