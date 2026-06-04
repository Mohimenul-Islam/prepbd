import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getTopics } from '../services/api';
import '../styles/Landing.css';

export default function Landing() {
  const navigate = useNavigate();
  const [topics, setTopics] = useState([]);
  const [totalQuestions, setTotalQuestions] = useState(0);

  useEffect(() => {
    getTopics()
      .then((data) => {
        setTopics(data);
        setTotalQuestions(data.reduce((sum, t) => sum + t.questionCount, 0));
      })
      .catch(() => {
        // API might not be running — show default data
        setTopics([
          { name: 'Data Structures', slug: 'data-structures', icon: '🏗️', questionCount: 30, description: 'Arrays, Linked Lists, Trees, Graphs, Sorting & Searching' },
          { name: 'Basic Programming', slug: 'basic-programming', icon: '💻', questionCount: 25, description: 'Variables, Loops, Functions, Recursion, Output Tracing' },
          { name: 'OOP', slug: 'oop', icon: '🎯', questionCount: 20, description: 'Encapsulation, Inheritance, Polymorphism, SOLID Principles' },
          { name: 'DBMS', slug: 'dbms', icon: '🗄️', questionCount: 25, description: 'SQL Queries, Normalization, ER Diagrams, ACID, Transactions' },
          { name: 'Operating Systems', slug: 'operating-systems', icon: '⚙️', questionCount: 20, description: 'Processes, Scheduling, Memory, Deadlocks, Synchronization' },
          { name: 'Networking', slug: 'networking', icon: '🌐', questionCount: 15, description: 'OSI Model, TCP/UDP, HTTP, DNS, Subnetting' },
          { name: 'Linux Commands', slug: 'linux-commands', icon: '🐧', questionCount: 15, description: 'File Operations, Permissions, Piping, Process Management' },
          { name: 'Git & GitHub', slug: 'git-github', icon: '🔀', questionCount: 12, description: 'Branching, Merging, Rebasing, Pull Requests, Workflows' },
          { name: 'AI & ML Basics', slug: 'ai-ml', icon: '🤖', questionCount: 12, description: 'Supervised/Unsupervised, Overfitting, Neural Networks' },
          { name: 'Analytical Reasoning', slug: 'analytical-reasoning', icon: '🧩', questionCount: 15, description: 'Logic Puzzles, Patterns, Quantitative Aptitude' },
        ]);
        setTotalQuestions(189);
      });
  }, []);

  return (
    <div className="landing">
      <div className="container">
        {/* Hero */}
        <section className="hero">
          <div className="hero-badge">
            <span className="dot"></span>
            BD Tech Company Interview Prep
          </div>
          <h1>
            Master CS Fundamentals with{' '}
            <span className="gradient-text">PrepBD</span>
          </h1>
          <p className="hero-subtitle">
            Practice written tests with AI-powered evaluation. Prepare for bKash bTechWhiz,
            Therap, Optimizely, Cefalo, and other top BD tech company recruitment exams.
          </p>
          <div className="hero-actions">
            <button className="btn btn-primary btn-lg" onClick={() => navigate('/test/setup')}>
              Start Mock Test →
            </button>
          </div>
        </section>

        {/* Stats */}
        <div className="stats">
          <div className="stat">
            <div className="stat-value">{totalQuestions}+</div>
            <div className="stat-label">Questions</div>
          </div>
          <div className="stat">
            <div className="stat-value">{topics.length}</div>
            <div className="stat-label">Topics</div>
          </div>
          <div className="stat">
            <div className="stat-value">AI</div>
            <div className="stat-label">Powered Evaluation</div>
          </div>
          <div className="stat">
            <div className="stat-value">Free</div>
            <div className="stat-label">No Sign-up</div>
          </div>
        </div>

        {/* Topics */}
        <section className="topics-section">
          <h2>Topics Covered</h2>
          <div className="topics-grid">
            {topics.map((topic, i) => (
              <div key={topic.slug} className={`topic-card animate-in delay-${i + 1}`}>
                <div className="topic-icon">{topic.icon}</div>
                <div className="topic-info">
                  <h3>{topic.name}</h3>
                  <p>{topic.description}</p>
                  <div className="topic-count">{topic.questionCount} questions</div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Features */}
        <section className="features-section">
          <h2>How It Works</h2>
          <div className="features-grid">
            <div className="feature-card animate-in delay-1">
              <div className="icon">📝</div>
              <h3>Written Answers Only</h3>
              <p>No multiple choice. Write your answers just like in the actual exam. Practice explaining concepts clearly and concisely.</p>
            </div>
            <div className="feature-card animate-in delay-2">
              <div className="icon">🤖</div>
              <h3>AI Evaluation</h3>
              <p>Get detailed feedback on every answer — what you got right, what you missed, and how to improve. Plus model answers for reference.</p>
            </div>
            <div className="feature-card animate-in delay-3">
              <div className="icon">🎯</div>
              <h3>Company-Tagged Questions</h3>
              <p>Questions are mapped to real patterns from bKash bTechWhiz, Therap, Optimizely, and Cefalo recruitment tests. Practice what actually gets asked.</p>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
