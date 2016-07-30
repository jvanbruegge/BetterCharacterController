using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
	[SerializeField]
	private float movementSpeed = 2f;

	private Vector3 movement;
	private List<KeyValuePair<int, Vector3>> vetos = new List<KeyValuePair<int, Vector3>>();

	public Vector3 Movement
	{
		get { return movement; }
	}

	public void addVeto(int weight, Vector3 alternative)
	{
		this.vetos.Add(new KeyValuePair<int, Vector3>(weight, alternative));
	}

	private void Update()
	{
		Vector3 movement = Vector3.zero;

		if(getForward())
		{
			movement += transform.forward;
		}
		else if(getBackward())
		{
			movement -= transform.forward;
		}

		if(getLeft())
		{
			movement -= transform.right;
		}
		else if(getRight())
		{
			movement += transform.right;
		}

		this.movement = movement.normalized * movementSpeed;
	}

	private void LateUpdate()
	{
		Vector3 desiredPosition = vetos.Aggregate(new KeyValuePair<int, Vector3>(0, movement * Time.deltaTime + transform.position), (acc, curr) => acc.Key < curr.Key ? curr : acc).Value;
		transform.position = desiredPosition;
		vetos.Clear();
	}

	//++++++++++++++++ Override those methods in a subclass to plug in a custom input logic +++++++++++++++++++++++//
	protected virtual bool getForward()		{ return Input.GetKey(KeyCode.W); }
	protected virtual bool getBackward()	{ return Input.GetKey(KeyCode.S); }
	protected virtual bool getLeft()		{ return Input.GetKey(KeyCode.A); }
	protected virtual bool getRight()		{ return Input.GetKey(KeyCode.D); }
}