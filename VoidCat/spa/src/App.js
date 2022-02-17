import {BrowserRouter, Routes, Route} from "react-router-dom";
import {FilePreview} from "./FilePreview";
import {HomePage} from "./HomePage";
import {Admin} from "./Admin/Admin";

import './App.css';

function App() {
    return (
        <div className="app">
            <BrowserRouter>
                <Routes>
                    <Route exact path="/" element={<HomePage/>}/>
                    <Route path="/admin" element={<Admin/>}/>
                    <Route exact path="/:id" element={<FilePreview/>}/>
                </Routes>
            </BrowserRouter>
        </div>
    );
}

export default App;
