using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoroutineManager : MonoBehaviour {
	public static CoroutineManager Instance {
		get;
		private set;
	}

	List<System.Collections.IEnumerator> m_enumerators = new List<System.Collections.IEnumerator>();
	List<System.Collections.IEnumerator> m_enumeratorsBuffer = new List<System.Collections.IEnumerator>();

	void Awake() {
		if(Instance == null) {
			Instance = this;
		} else {
			Debug.LogError("Multi-instances of CoroutineManager");
		}
	}

	void LateUpdate() {
		for(int i = 0; i < m_enumerators.Count; ++i) {
			// handle special enumerator
			if(m_enumerators[i].Current is CoroutineYieldInstruction) {
				CoroutineYieldInstruction yieldInstruction = m_enumerators[i].Current as CoroutineYieldInstruction;
				if(!yieldInstruction.IsDone()) {
					continue;
				}
			}

			// other special enumerator here ...
			// do normal move next
			// remove the enumerator when it return false
			// MoveNext return false when end
			if(!m_enumerators[i].MoveNext()) {
				m_enumeratorsBuffer.Add(m_enumerators[i]);
				continue;
			}
		}

		// remove end enumerator
		for(int i = 0; i < m_enumeratorsBuffer.Count; ++i) {
			m_enumerators.Remove(m_enumeratorsBuffer[i]);
		}
		m_enumeratorsBuffer.Clear();
	}

	public void StartCoroutineSimple(System.Collections.IEnumerator enumerator) {
		m_enumerators.Add(enumerator);
	}
}
