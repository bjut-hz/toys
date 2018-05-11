using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine(UnityCoroutine());
		CoroutineManager.Instance.StartCoroutineSimple(SelfCoroutine());
	}
	
	// Update is called once per frame
	void Update () {
	}
	IEnumerator UnityCoroutine() {
		Debug.Log("Unity coroutine begin at time : " + Time.time);
		yield return new WaitForSeconds(5);
		Debug.Log("Unity coroutine begin at time : " + Time.time);
	}

	IEnumerator SelfCoroutine() {
		Debug.Log("Self coroutine begin at time : " + Time.time);
		yield return new CoroutineWaitForSeconds(5);
		Debug.Log("Self coroutine begin at time : " + Time.time);
	}
}
