import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { evaluateAnswers } from '../services/api';
import '../styles/Results.css';

export default function Results() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [evaluation, setEvaluation] = useState(null);
  const [testData, setTestData] = useState(null);
  const [expandedCards, setExpandedCards] = useState({});

  useEffect(() => {
    const saved = sessionStorage.getItem('testResults');
    if (!saved) {
      navigate('/test/setup');
      return;
    }

    const data = JSON.parse(saved);
    setTestData(data);

    // Build answers for API
    const answerPayload = data.questions.map((q) => ({
      questionId: q.id,
      userAnswer: data.answers[q.id] || '',
    }));

    evaluateAnswers(answerPayload)
      .then((result) => {
        setEvaluation(result);
        setLoading(false);
        // Expand first card by default
        if (result.evaluations?.length > 0) {
          setExpandedCards({ 0: true });
        }
      })
      .catch(() => {
        // Fallback: show basic results without AI
        setEvaluation({
          summary: 'Could not connect to the evaluation server. Please make sure the API is running and try again.',
          evaluations: data.questions.map((q) => ({
            questionId: q.id,
            question: q.questionText || q.question,
            userAnswer: data.answers[q.id] || '[Not attempted]',
            accuracy: 'AI evaluation unavailable.',
            gaps: 'Could not evaluate.',
            improvement: 'Try again with the API server running.',
            modelAnswer: 'Model answer unavailable in offline mode.',
          })),
        });
        setLoading(false);
        setExpandedCards({ 0: true });
      });
  }, [navigate]);

  function toggleCard(index) {
    setExpandedCards((prev) => ({ ...prev, [index]: !prev[index] }));
  }

  function formatTime(seconds) {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}m ${s}s`;
  }

  function renderContent(text) {
    if (!text) return '';
    // Handle code blocks in evaluation text
    const parts = text.split(/(```[\s\S]*?```)/g);
    return parts.map((part, i) => {
      if (part.startsWith('```')) {
        const code = part.replace(/```\w*\n?/, '').replace(/```$/, '');
        return <pre key={i}><code>{code}</code></pre>;
      }
      return <span key={i}>{part}</span>;
    });
  }

  if (loading) {
    return (
      <div className="results">
        <div className="container-narrow">
          <div className="loading-screen">
            <div className="loading-spinner"></div>
            <h2>Evaluating Your Answers...</h2>
            <p>AI is reviewing each answer in detail. This may take a moment.</p>
          </div>
        </div>
      </div>
    );
  }

  const answeredCount = testData
    ? Object.values(testData.answers).filter((a) => a?.trim().length > 0).length
    : 0;

  return (
    <div className="results">
      <div className="container-narrow">
        <div className="results-header animate-in">
          <h1>📊 Test Results</h1>
        </div>

        {/* Summary */}
        <div className="summary-card animate-in delay-1">
          <h2>📋 Overall Summary</h2>
          <div className="summary-text">{evaluation?.summary}</div>
          <div className="summary-stats">
            <div className="summary-stat">
              <div className="summary-stat-value" style={{ color: 'var(--accent-primary)' }}>
                {testData?.questions.length}
              </div>
              <div className="summary-stat-label">Total Questions</div>
            </div>
            <div className="summary-stat">
              <div className="summary-stat-value" style={{ color: 'var(--success)' }}>
                {answeredCount}
              </div>
              <div className="summary-stat-label">Answered</div>
            </div>
            <div className="summary-stat">
              <div className="summary-stat-value" style={{ color: 'var(--warning)' }}>
                {(testData?.questions.length || 0) - answeredCount}
              </div>
              <div className="summary-stat-label">Unanswered</div>
            </div>
            {testData?.timeTaken && (
              <div className="summary-stat">
                <div className="summary-stat-value" style={{ color: 'var(--text-primary)' }}>
                  {formatTime(testData.timeTaken)}
                </div>
                <div className="summary-stat-label">Time Taken</div>
              </div>
            )}
          </div>
        </div>

        {/* Per-Question Evaluations */}
        <div className="eval-list">
          {evaluation?.evaluations?.map((ev, i) => (
            <div key={i} className={`eval-card animate-in delay-${Math.min(i + 2, 10)}`}>
              <div className="eval-card-header" onClick={() => toggleCard(i)}>
                <div className="eval-card-title">
                  <span className="eval-q-num">Q{i + 1}</span>
                  {testData?.questions[i] && (
                    <>
                      <span className="badge badge-topic">{testData.questions[i].topic}</span>
                      <span className={`badge badge-${testData.questions[i].difficulty}`}>
                        {testData.questions[i].difficulty}
                      </span>
                    </>
                  )}
                </div>
                <span className={`eval-toggle ${expandedCards[i] ? 'open' : ''}`}>▼</span>
              </div>

              {expandedCards[i] && (
                <div className="eval-card-body">
                  {/* Question */}
                  <div className="eval-section">
                    <div className="eval-section-label question">📝 Question</div>
                    <div className="eval-section-content">
                      {renderContent(ev.question)}
                    </div>
                  </div>

                  {/* User Answer */}
                  <div className="eval-section">
                    <div className="eval-section-label your-answer">✍️ Your Answer</div>
                    <div className="eval-section-content user-answer">
                      {ev.userAnswer?.trim() || '[Not attempted]'}
                    </div>
                  </div>

                  {/* Accuracy */}
                  <div className="eval-section">
                    <div className="eval-section-label accuracy">✅ Accuracy</div>
                    <div className="eval-section-content accuracy-content">
                      {renderContent(ev.accuracy)}
                    </div>
                  </div>

                  {/* Gaps */}
                  <div className="eval-section">
                    <div className="eval-section-label gaps">⚠️ Gaps</div>
                    <div className="eval-section-content gaps-content">
                      {renderContent(ev.gaps)}
                    </div>
                  </div>

                  {/* Improvement */}
                  <div className="eval-section">
                    <div className="eval-section-label improvement">💡 How to Improve</div>
                    <div className="eval-section-content improvement-content">
                      {renderContent(ev.improvement)}
                    </div>
                  </div>

                  {/* Model Answer */}
                  <div className="eval-section">
                    <div className="eval-section-label model">📝 Model Answer</div>
                    <div className="eval-section-content model-content">
                      {renderContent(ev.modelAnswer)}
                    </div>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>

        {/* Actions */}
        <div className="results-actions">
          <button className="btn btn-secondary btn-lg" onClick={() => navigate('/test/setup')}>
            New Test
          </button>
          <button className="btn btn-primary btn-lg" onClick={() => navigate('/')}>
            Back Home
          </button>
        </div>
      </div>
    </div>
  );
}
