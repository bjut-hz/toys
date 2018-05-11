using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class CoroutineWaitForSeconds : CoroutineYieldInstruction {
	float m_waitTime;
	float m_startTime;
	public CoroutineWaitForSeconds(float waitTime) {
		m_waitTime = waitTime;
		m_startTime = -1;
	}

	public override bool IsDone() {
		// NOTE: a little tricky here
		if(m_startTime < 0) {
			m_startTime = Time.time;
		}

		// check elapsed time
		return (Time.time - m_startTime) >= m_waitTime;
	}
}
