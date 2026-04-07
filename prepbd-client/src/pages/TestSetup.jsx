import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getTopics, getQuestions } from '../services/api';
import '../styles/TestSetup.css';

const QUESTION_COUNTS = [5, 10, 15, 20];
const TIMER_OPTIONS = [
  { value: 0, label: 'No Timer' },
  { value: 30, label: '30 min' },
  { value: 45, label: '45 min' },
  { value: 60, label: '60 min' },
  { value: 90, label: '90 min' },
];

const DEFAULT_TOPICS = [
  { name: 'Data Structures', slug: 'data-structures', icon: '🏗️', questionCount: 30 },
  { name: 'Basic Programming', slug: 'basic-programming', icon: '💻', questionCount: 25 },
  { name: 'OOP', slug: 'oop', icon: '🎯', questionCount: 20 },
  { name: 'DBMS', slug: 'dbms', icon: '🗄️', questionCount: 25 },
  { name: 'Operating Systems', slug: 'operating-systems', icon: '⚙️', questionCount: 20 },
  { name: 'Networking', slug: 'networking', icon: '🌐', questionCount: 15 },
  { name: 'Linux Commands', slug: 'linux-commands', icon: '🐧', questionCount: 15 },
  { name: 'Git & GitHub', slug: 'git-github', icon: '🔀', questionCount: 12 },
  { name: 'AI & ML Basics', slug: 'ai-ml', icon: '🤖', questionCount: 12 },
  { name: 'Analytical Reasoning', slug: 'analytical-reasoning', icon: '🧩', questionCount: 15 },
];

export default function TestSetup() {
  const navigate = useNavigate();
  const [topics, setTopics] = useState(DEFAULT_TOPICS);
  const [mode, setMode] = useState('mixed');
  const [selectedTopics, setSelectedTopics] = useState([]);
  const [questionCount, setQuestionCount] = useState(10);
  const [timerMinutes, setTimerMinutes] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    getTopics().then(setTopics).catch(() => {});
  }, []);

  function toggleTopic(slug) {
    setSelectedTopics((prev) =>
      prev.includes(slug) ? prev.filter((s) => s !== slug) : [...prev, slug]
    );
  }

  async function startTest() {
    setError('');
    if (mode === 'single' && selectedTopics.length !== 1) {
      setError('Please select exactly one topic for single-topic mode.');
      return;
    }
    if (mode === 'selected' && selectedTopics.length < 2) {
      setError('Please select at least 2 topics.');
      return;
    }

    setLoading(true);
    try {
      const topicSlugs = mode === 'mixed' ? [] : selectedTopics;
      const questions = await getQuestions(mode, topicSlugs, questionCount);

      if (questions.length === 0) {
        setError('No questions available for the selected topics.');
        setLoading(false);
        return;
      }

      // Save test config to sessionStorage
      sessionStorage.setItem(
        'testConfig',
        JSON.stringify({
          mode,
          selectedTopics: topicSlugs,
          questionCount,
          timerMinutes,
          questions,
          startTime: Date.now(),
        })
      );

      navigate('/test/exam');
    } catch {
      setError('Could not load questions. Make sure the API server is running.');
      setLoading(false);
    }
  }

  const showTopicPicker = mode === 'single' || mode === 'selected';

  return (
    <div className="setup">
      <div className="container-narrow">
        <div className="setup-header animate-in">
          <button className="btn btn-ghost" onClick={() => navigate('/')}>← Back</button>
          <h1>Configure Your Test</h1>
          <p>Choose your test mode, topics, and settings</p>
        </div>

        {/* Mode Selection */}
        <div className="setup-section animate-in delay-1">
          <h2>Test Mode</h2>
          <div className="mode-grid">
            <div className={`mode-card ${mode === 'mixed' ? 'selected' : ''}`} onClick={() => { setMode('mixed'); setSelectedTopics([]); }}>
              <div className="mode-icon">🔀</div>
              <h3>Mixed</h3>
              <p>Random questions from all topics</p>
            </div>
            <div className={`mode-card ${mode === 'single' ? 'selected' : ''}`} onClick={() => { setMode('single'); setSelectedTopics([]); }}>
              <div className="mode-icon">🎯</div>
              <h3>Single Topic</h3>
              <p>Focus on one specific topic</p>
            </div>
            <div className={`mode-card ${mode === 'selected' ? 'selected' : ''}`} onClick={() => { setMode('selected'); setSelectedTopics([]); }}>
              <div className="mode-icon">✅</div>
              <h3>Select Topics</h3>
              <p>Pick 2 or more topics to mix</p>
            </div>
          </div>
        </div>

        {/* Topic Picker */}
        {showTopicPicker && (
          <div className="setup-section animate-fade">
            <h2>{mode === 'single' ? 'Select One Topic' : 'Select Topics (2+)'}</h2>
            <div className="topic-select-grid">
              {topics.map((topic) => (
                <div
                  key={topic.slug}
                  className={`topic-select-card ${selectedTopics.includes(topic.slug) ? 'selected' : ''}`}
                  onClick={() => {
                    if (mode === 'single') {
                      setSelectedTopics([topic.slug]);
                    } else {
                      toggleTopic(topic.slug);
                    }
                  }}
                >
                  <div className="check">{selectedTopics.includes(topic.slug) ? '✓' : ''}</div>
                  <span className="topic-emoji">{topic.icon}</span>
                  <span className="topic-label">{topic.name}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Count & Timer */}
        <div className="setup-section animate-in delay-2">
          <div className="option-row">
            <div className="option-group">
              <h2>Questions</h2>
              <div className="option-buttons">
                {QUESTION_COUNTS.map((c) => (
                  <button key={c} className={`option-btn ${questionCount === c ? 'selected' : ''}`} onClick={() => setQuestionCount(c)}>
                    {c}
                  </button>
                ))}
              </div>
            </div>
            <div className="option-group">
              <h2>Timer</h2>
              <div className="option-buttons">
                {TIMER_OPTIONS.map((t) => (
                  <button key={t.value} className={`option-btn ${timerMinutes === t.value ? 'selected' : ''}`} onClick={() => setTimerMinutes(t.value)}>
                    {t.label}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Start */}
        {error && <div className="error-msg">{error}</div>}
        <div className="setup-actions animate-in delay-3">
          <button className="btn btn-primary btn-lg" onClick={startTest} disabled={loading}>
            {loading ? 'Loading Questions...' : 'Start Test →'}
          </button>
        </div>
      </div>
    </div>
  );
}
