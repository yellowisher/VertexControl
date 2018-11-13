using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleController : MonoBehaviour
{
	private EditableThing currentDragging;

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit result;
			if (Physics.Raycast(ray, out result))
			{
				EditableThing thing = result.collider.GetComponent<EditableThing>();
				if (thing != null)
				{
					currentDragging = thing;
				}
			}
		}
		else if (Input.GetMouseButton(0))
		{
			if (currentDragging != null)
			{
				Vector3 mousePosition = Input.mousePosition;
				mousePosition.z = currentDragging.transform.position.z - Camera.main.transform.position.z;

				Vector3 desiredPosition = Camera.main.ScreenToWorldPoint(mousePosition);
				desiredPosition.z = currentDragging.transform.position.z;
				currentDragging.UpdatePosition(desiredPosition);
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			currentDragging = null;
		}
	}
}
