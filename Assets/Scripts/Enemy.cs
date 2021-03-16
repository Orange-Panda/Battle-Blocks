using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : NetworkComponent
{
	public const string StatusMessage = "S";

	private int Health { get; set; } = 3;
	private const int MaxHealth = 3; 

	private void OnEnable()
	{
		NetworkCore.ActiveNetwork.NetworkTick += ActiveNetwork_NetworkTick;
	}

	private void OnDisable()
	{
		NetworkCore.ActiveNetwork.NetworkTick -= ActiveNetwork_NetworkTick;
	}

	private void ActiveNetwork_NetworkTick()
	{
		if (IsServer)
		{
			SendToServer(StatusMessage, Health.ToString());
		}
	}

	public override void HandleMessage(string command, List<string> args)
	{
		if (command.Equals(StatusMessage) && args.Count >= 1 && int.TryParse(args[0], out int value))
		{
			Health = value;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.TryGetComponent(out Tank tank))
		{
			tank.TakeDamage();
		}
	}

	public void TakeDamage(int value = 1)
	{
		if (IsServer)
		{
			Health = Mathf.Clamp(Health - value, 0, MaxHealth);

			if (Health <= 0)
			{
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
		}
	}

	protected override IEnumerator NetworkUpdate()
	{
		if (IsServer)
		{
			Spawner spawner = FindObjectOfType<Spawner>();
			NavMeshAgent agent = gameObject.AddComponent<NavMeshAgent>();
			agent.radius = 0.1f;
			agent.speed = Random.Range(2f, 6f);

			while (IsServer)
			{
				List<Vector3> pointList = new List<Vector3>(spawner.enemyPoints);
				for (int i = 0; i < pointList.Count; i++)
				{
					int randIndex = Random.Range(0, pointList.Count);
					Vector3 temp = pointList[randIndex];
					pointList[randIndex] = pointList[i];
					pointList[i] = temp;
				}
				Queue<Vector3> points = new Queue<Vector3>(pointList);

				while (points.Count > 0)
				{
					Vector3 goal = points.Dequeue();
					agent.destination = goal;
					yield return new WaitUntil(() => Vector3.Distance(transform.position, goal) < 1);
				}
			}
		}
		yield break;
	}
}