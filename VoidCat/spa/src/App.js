import './App.css';

function App() {
    
    function selectFiles(e) {
        
    }
  return (
      <div className="app">
          <div className="drop" onClick={selectFiles}>
              <h3>Drop files here!</h3>
          </div>
    </div>
  );
}

export default App;
