# PrepBD 🇧🇩

Practice written tests for Bangladeshi tech company interviews with AI-powered evaluation.

## What is this?
PrepBD is a mock testing platform designed specifically for fresh CS graduates preparing for recruitment exams at top tech companies in Bangladesh (e.g., bKash bTechWiz, Therap). It focuses on written-answer questions, which are frequently used in these exams, instead of standard multiple-choice questions.

## Tech Stack
- **Backend:** ASP.NET Core 10 Web API, any OpenAI-compatible LLM (OpenRouter / OpenAI / Groq / …)
- **Frontend:** React 19, Vite, Vanilla CSS
- **Question Bank:** 260 curated CS questions across 10 topics

## Features
- **Written-Answers Only:** Practice explaining concepts clearly and writing code snippets directly.
- **AI-Powered Evaluation:** Get a per-question score (0–10) plus detailed feedback (what you got right, what you missed, how to improve). For coding questions the examiner mentally runs your code against sample cases.
- **Pluggable LLM Provider:** Works with any OpenAI-compatible API — switch between OpenRouter, OpenAI, Groq, etc. by editing config, no code change.
- **Flexible Test Modes:** Take tests on a single topic, mix all topics, or select a few.
- **Configurable Settings:** Set timer duration and number of questions.
- **Graceful Degradation:** Works offline and falls back to displaying model answers if the AI service isn't configured.

## Screenshots

*(Add screenshots of your application here)*

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- An LLM API key from any OpenAI-compatible provider (optional — app will gracefully fallback to showing model answers). A free [OpenRouter](https://openrouter.ai) key works out of the box.

### Backend
1. Navigate to the API folder:
   ```bash
   cd PrepBD.Api
   ```
2. Configure your LLM API key (using `dotnet user-secrets` to keep it secure):
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Llm:ApiKey" "your-api-key-here"
   ```
   The provider and model are set in `appsettings.json` under `Llm` (`BaseUrl`, `Model`). The
   default is a free OpenRouter model; point `BaseUrl`/`Model` at any OpenAI-compatible API to switch.
3. Run the API (it will typically start on `http://localhost:5177`):
   ```bash
   dotnet run
   ```

### Frontend
1. Navigate to the client folder:
   ```bash
   cd prepbd-client
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Run the development server (starts on `http://localhost:5173`):
   ```bash
   npm run dev
   ```

## Design System
Built from scratch with vanilla CSS utilizing a custom dark space UI theme. It features glassmorphism, responsive CSS-grid layouts, and customized scrollbars. No CSS frameworks like Tailwind or Bootstrap were used.
