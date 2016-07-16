using UnityEngine;
using UnityEditor;

using System.Collections;

namespace mattatz.TeddySystem.Example {

	[CustomEditor (typeof(Drawer))]
	public class DrawerEditor : Editor {

		public override void OnInspectorGUI () {
			base.OnInspectorGUI();
			if(GUILayout.Button("Save")) {
				(target as Drawer).Save();
			}
		}

	}

}

