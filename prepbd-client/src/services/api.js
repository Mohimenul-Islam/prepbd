const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5177';

export async function getTopics() {
  const res = await fetch(`${API_BASE}/api/questions/topics`);
  if (!res.ok) throw new Error('Failed to fetch topics');
  return res.json();
}

export async function getQuestions(mode, topics, count) {
  const params = new URLSearchParams({ mode, count: count.toString() });
  if (topics && topics.length > 0) {
    params.set('topics', topics.join(','));
  }
  const res = await fetch(`${API_BASE}/api/questions?${params}`);
  if (!res.ok) throw new Error('Failed to fetch questions');
  return res.json();
}

export async function evaluateAnswers(answers) {
  const res = await fetch(`${API_BASE}/api/evaluate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ answers }),
  });
  if (!res.ok) throw new Error('Failed to evaluate answers');
  return res.json();
}

export async function getEvaluationStatus() {
  const res = await fetch(`${API_BASE}/api/evaluate/status`);
  if (!res.ok) throw new Error('Failed to check status');
  return res.json();
}
