using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankBullet : NetworkComponent
{
	private float speed = 15;
	private bool used = false;

	public override void HandleMessage(string command, List<string> args)
	{
		
	}

	protected override IEnumerator NetworkUpdate()
	{
		if (transform.TryGetComponent(out Rigidbody rigidbody) && IsClient)
		{
			Destroy(rigidbody);
		}

		yield return new WaitForSeconds(10f);

		if (IsServer && !used)
		{
			used = true;
			NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
		}
	}

	private void Update()
	{
		if (IsServer)
		{
			transform.position += transform.forward * speed * Time.deltaTime;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsServer && !used)
		{
			if (other.gameObject.TryGetComponent(out Enemy enemy))
			{
				used = true;
				enemy.TakeDamage(Owner);
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
			else if (other.gameObject.TryGetComponent(out Tank tank) && tank.Owner != Owner)
			{
				used = true;
				tank.TakeDamage(Owner);
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
			else if (other.CompareTag("Wall"))
			{
				used = true;
				NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
			}
		}
	}
}