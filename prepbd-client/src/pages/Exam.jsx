import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import '../styles/Exam.css';

function getInitialConfig() {
  try {
    const saved = sessionStorage.getItem('testConfig');
    return saved ? JSON.parse(saved) : null;
  } catch {
    return null;
  }
}

function getInitialAnswers() {
  try {
    const saved = sessionStorage.getItem('testAnswers');
    return saved ? JSON.parse(saved) : {};
  } catch {
    return {};
  }
}

function getInitialTimeLeft(config) {
  if (!config || config.timerMinutes <= 0) return null;
  const elapsed = Math.floor((Date.now() - config.startTime) / 1000);
  const remaining = config.timerMinutes * 60 - elapsed;
  return Math.max(0, remaining);
}

export default function Exam() {
  const navigate = useNavigate();
  const [config] = useState(getInitialConfig);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState(getInitialAnswers);
  const [showModal, setShowModal] = useState(false);
  const [timeLeft, setTimeLeft] = useState(() => getInitialTimeLeft(getInitialConfig()));

  // Redirect if test config is missing
  useEffect(() => {
    if (!config) {
      navigate('/test/setup');
    }
  }, [config, navigate]);

  const handleSubmit = useCallback(() => {
    if (!config) return;
    sessionStorage.setItem(
      'testResults',
      JSON.stringify({
        questions: config.questions,
        answers,
        timeTaken: config.timerMinutes > 0
          ? config.timerMinutes * 60 - (timeLeft || 0)
          : Math.floor((Date.now() - config.startTime) / 1000),
      })
    );
    sessionStorage.removeItem('testAnswers');
    navigate('/test/results');
  }, [config, answers, timeLeft, navigate]);

  // Timer countdown
  useEffect(() => {
    if (timeLeft === null || timeLeft <= 0) return;
    const interval = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          handleSubmit();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(interval);
  }, [timeLeft, handleSubmit]);

  // Auto-save answers
  useEffect(() => {
    if (Object.keys(answers).length > 0) {
      sessionStorage.setItem('testAnswers', JSON.stringify(answers));
    }
  }, [answers]);

  if (!config) return null;

  const questions = config.questions;
  const current = questions[currentIndex];
  const answeredCount = Object.values(answers).filter((a) => a.trim().length > 0).length;
  const progress = ((currentIndex + 1) / questions.length) * 100;

  function formatTime(seconds) {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }

  function getTimerClass() {
    if (timeLeft === null) return '';
    if (timeLeft <= 60) return 'danger';
    if (timeLeft <= 300) return 'warning';
    return '';
  }

  function renderQuestion(text) {
    // Split on code blocks
    const parts = text.split(/(```[\s\S]*?```)/g);
    return parts.map((part, i) => {
      if (part.startsWith('```')) {
        const code = part.replace(/```\w*\n?/, '').replace(/```$/, '');
        return <pre key={i}><code>{code}</code></pre>;
      }
      return <span key={i}>{part}</span>;
    });
  }

  return (
    <div className="exam">
      {/* Top Bar */}
      <div className="exam-topbar">
        <div className="exam-topbar-left">
          <span className="exam-progress-text">
            <strong>{currentIndex + 1}</strong> / {questions.length}
          </span>
          <div className="exam-progress-bar">
            <div className="exam-progress-fill" style={{ width: `${progress}%` }}></div>
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-md)' }}>
          {timeLeft !== null && (
            <div className={`timer ${getTimerClass()}`}>
              🕐 {formatTime(timeLeft)}
            </div>
          )}
          <button className="btn btn-secondary btn-sm" onClick={() => setShowModal(true)}>
            Submit Test
          </button>
        </div>
      </div>

      <div className="exam-body">
        {/* Sidebar */}
        <div className="exam-sidebar">
          {questions.map((_, i) => (
            <button
              key={i}
              className={`q-dot ${i === currentIndex ? 'current' : ''} ${answers[questions[i].id]?.trim() ? 'answered' : ''}`}
              onClick={() => setCurrentIndex(i)}
            >
              {i + 1}
            </button>
          ))}
        </div>

        {/* Question Content */}
        <div className="exam-content">
          <div className="question-header">
            <span className="question-number">Question {currentIndex + 1}</span>
            <span className="badge badge-topic">{current.topic}</span>
            {current.subtopic && <span className="badge badge-topic">{current.subtopic}</span>}
            <span className={`badge badge-${current.difficulty}`}>{current.difficulty}</span>
          </div>

          <div className="question-text">
            {renderQuestion(current.questionText || current.question)}
          </div>

          {current.examples && current.examples.length > 0 && (
            <div className="examples">
              <div className="examples-label">Examples</div>
              {current.examples.map((ex, i) => (
                <div key={i} className="example-row">
                  <div><span className="example-key">Input:</span> <code>{ex.input}</code></div>
                  <div><span className="example-key">Output:</span> <code>{ex.output}</code></div>
                  {ex.explanation && <div className="example-explain">{ex.explanation}</div>}
                </div>
              ))}
            </div>
          )}

          {current.leetcodeUrl && (
            <a
              className="leetcode-btn"
              href={current.leetcodeUrl}
              target="_blank"
              rel="noreferrer"
            >
              Solve a similar problem on LeetCode ↗
            </a>
          )}

          {current.hints && current.hints.length > 0 && (
            <div style={{ marginBottom: 'var(--space-lg)', padding: 'var(--space-md)', background: 'var(--info-bg)', border: '1px solid var(--info-border)', borderRadius: 'var(--radius-sm)' }}>
              <strong style={{ color: 'var(--info)' }}>💡 Hint:</strong>{' '}
              <span style={{ color: 'var(--text-secondary)' }}>{current.hints[0]}</span>
            </div>
          )}

          <div className="answer-section">
            <h3>Your Answer</h3>
            <textarea
              className="answer-textarea"
              value={answers[current.id] || ''}
              onChange={(e) => setAnswers({ ...answers, [current.id]: e.target.value })}
              placeholder="Write your answer here..."
              autoFocus
            />
            <div className="answer-meta">
              <span>{(answers[current.id] || '').length} characters</span>
              <span>{answers[current.id]?.trim() ? '✓ Answered' : 'Not answered yet'}</span>
            </div>
          </div>

          <div className="exam-nav">
            <button
              className="btn btn-secondary"
              onClick={() => setCurrentIndex(Math.max(0, currentIndex - 1))}
              disabled={currentIndex === 0}
            >
              ← Previous
            </button>
            {currentIndex < questions.length - 1 ? (
              <button
                className="btn btn-primary"
                onClick={() => setCurrentIndex(currentIndex + 1)}
              >
                Next →
              </button>
            ) : (
              <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                Submit Test
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Submit Modal */}
      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Submit Test?</h2>
            <p>Review your progress before submitting.</p>
            <div className="modal-stats">
              <div className="modal-stat">
                <div className={`modal-stat-value ${answeredCount === questions.length ? 'good' : 'warn'}`}>
                  {answeredCount}/{questions.length}
                </div>
                <div className="modal-stat-label">Answered</div>
              </div>
              <div className="modal-stat">
                <div className="modal-stat-value" style={{ color: 'var(--text-primary)' }}>
                  {questions.length - answeredCount}
                </div>
                <div className="modal-stat-label">Unanswered</div>
              </div>
            </div>
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setShowModal(false)}>
                Continue Test
              </button>
              <button className="btn btn-primary" onClick={handleSubmit}>
                Submit Now
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
