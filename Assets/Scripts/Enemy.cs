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

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.TryGetComponent(out Tank tank))
		{
			tank.TakeDamage(-10);
		}
	}

	public void TakeDamage(int attacker, int value = 1)
	{
		if (IsServer)
		{
			Health = Mathf.Clamp(Health - value, 0, MaxHealth);

			if (Health <= 0)
			{
				foreach (Tank tank in Tank.Tanks)
				{
					if (tank.Owner == attacker)
					{
						tank.GrantScore(1);
					}
				}
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
			agent.speed = Random.Range(4f, 8f);

			while (IsServer)
			{
				Vector3 goal = spawner.enemyPoints[Random.Range(0, spawner.enemyPoints.Length)];
				agent.destination = goal;
				yield return new WaitUntil(() => Vector3.Distance(transform.position, goal) < 1);
				yield return new WaitForSeconds(Random.Range(0.4f, 2f));
			}
		}
	}
}