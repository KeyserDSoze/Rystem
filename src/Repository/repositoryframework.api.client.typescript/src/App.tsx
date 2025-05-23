import './App.css';
import logo from './logo.svg';
import { Runner, RunnerWithAny } from './Tester/Runner';
import { Setup } from "./Tester/Setup";

function App() {
    Setup();
    Runner();
    RunnerWithAny();
    return (
        <div className="App">
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo" />
                <p>
                    Edit <code>src/App.tsx</code> and save to reload.
                </p>
                <a
                    className="App-link"
                    href="https://reactjs.org"
                    target="_blank"
                    rel="noopener noreferrer"
                >
                    Learn React
                </a>
            </header>
        </div>
    );
}

export default App;
