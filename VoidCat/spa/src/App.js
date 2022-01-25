import './App.css';
import {FilePreview} from "./FilePreview";
import {Uploader} from "./Uploader";

function App() {
    let hasPath = window.location.pathname !== "/";
    return hasPath ? <FilePreview id={window.location.pathname.substr(1)}/> : <Uploader/>;
}

export default App;
