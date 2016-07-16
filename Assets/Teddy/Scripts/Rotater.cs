using UnityEngine;
using System.Collections;

namespace mattatz.TeddySystem.Demo {

	public class Rotater : MonoBehaviour {

		[SerializeField] float speed = 1f;

		void Start () {
		}
		
		void Update () {
		}

		void FixedUpdate () {
			transform.localRotation *= Quaternion.AngleAxis(speed * Time.fixedDeltaTime, transform.up);
		}

	}

}

